using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Hubs.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class UserMessageService : IUserMessageService
    {
        private readonly IUserMessageRepository _repo;
        private readonly IProfanityService _profanityService;
        private readonly IMessageCorrelationService _messageCorrelationService;
        private readonly IMessageTriageService _messageTriageService;
        private readonly IUserMessageAdminQueueRepository _adminQueueRepository;
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly ILogger<UserMessageService> _logger;

        public UserMessageService(IUserMessageRepository repo, IProfanityService profanityService, IMessageCorrelationService messageCorrelationService, IMessageTriageService messageTriageService, IUserMessageAdminQueueRepository adminQueueRepository, IHubContext<MessageHub> hubContext, ILogger<UserMessageService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _profanityService = profanityService ?? throw new ArgumentNullException(nameof(profanityService));
            _messageCorrelationService = messageCorrelationService ?? throw new ArgumentNullException(nameof(messageCorrelationService));
            _messageTriageService = messageTriageService ?? throw new ArgumentNullException(nameof(messageTriageService));
            _adminQueueRepository = adminQueueRepository ?? throw new ArgumentNullException(nameof(adminQueueRepository));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /*
         * Compatibility overload.
         *
         * Existing callers do not automatically
         * request an administrative review.
         */
        public Task<UserMessage> InsertAsync(UserMessage msg, CancellationToken ct = default)
        {
            return InsertAsync(msg, requestAdminReview: false, ct);
        }


        public async Task<UserMessage> InsertAsync(UserMessage msg, bool requestAdminReview, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(msg);

            if (string.IsNullOrWhiteSpace(msg.Content))
            {
                throw new ArgumentException("Content cannot be empty.", nameof(msg));
            }

            msg.Content = msg.Content.Trim();

            // ----------------------------------
            // 1. Moderation / profanity
            // ----------------------------------

            var profanity = await _profanityService.AnalyzeAsync(msg.Content, ct);

            if (profanity.ShouldReject)
            {
                throw new ArgumentException("The message contains prohibited content. " + $"Score={profanity.Score}.", nameof(msg.Content));
            }

            // ----------------------------------
            // 2. Normalize input
            // ----------------------------------

            msg.UserId = string.IsNullOrWhiteSpace(msg.UserId) ? "anon" : msg.UserId.Trim();
            msg.SourceType = string.IsNullOrWhiteSpace(msg.SourceType) ? "Other" : msg.SourceType.Trim();
            msg.RelatedName = string.IsNullOrWhiteSpace(msg.RelatedName) ? null : msg.RelatedName.Trim();
            msg.Tags = string.IsNullOrWhiteSpace(msg.Tags) ? null : msg.Tags.Trim();

            // ----------------------------------
            // 3. Geographic/entity correlation
            // ----------------------------------

            var correlated = await _messageCorrelationService.CorrelateAsync(msg, ct);

            // ----------------------------------
            // 4. Persist FIRST
            // ----------------------------------

            var created = await _repo.InsertAsync(correlated, ct);

            if (created.Id <= 0)
            {
                throw new InvalidOperationException("UserMessage could not be persisted.");
            }

            // ----------------------------------
            // 5. Administrative triage
            // ----------------------------------

            var triage = await _messageTriageService.AnalyzeAsync(created, ct);

            _logger.LogInformation(
                "[MESSAGE-TRIAGE] " +
                "MessageId={MessageId}; " +
                "RequestAdminReview={RequestAdminReview}; " +
                "RequiresAdminReview={RequiresAdminReview}; " +
                "Category={Category}; " +
                "Priority={Priority}; " +
                "Confidence={Confidence}; " +
                "Source={ClassificationSource}",
                created.Id,
                requestAdminReview,
                triage.RequiresAdminReview,
                triage.Category,
                triage.Priority,
                triage.Confidence,
                triage.ClassificationSource);
            /*
             * Two independent reasons can put
             * the message into the administrative queue:
             *
             * 1. OutZen rules detected a problem.
             * 2. The user explicitly requested admin review.
             */
            var requiresAdminReview = requestAdminReview || triage.RequiresAdminReview;

            _logger.LogInformation(
                "[MESSAGE-TRIAGE] " +
                "Final RequiresAdminReview={RequiresAdminReview} " +
                "for MessageId={MessageId}",
                requiresAdminReview,
                created.Id);

            if (requiresAdminReview)
            {
                var category = triage.Category != AdminMessageCategory.Unknown ? triage.Category : AdminMessageCategory.OtherAdministrative;
                var priority = triage.Priority > 0 ? triage.Priority : (byte)1;
                var classificationSource = requestAdminReview ? triage.RequiresAdminReview ? "User+Rules" : "UserExplicit" : triage.ClassificationSource;
                var confidence = requestAdminReview && !triage.RequiresAdminReview ? 1.0m : (decimal) Math.Clamp(triage.Confidence, 0d, 1d);
                var queueItem =
                    new UserMessageAdminQueue
                    {
                        MessageId = created.Id,
                        Category = category,
                        Priority = priority,
                        Status = AdminMessageStatus.Open,
                        Confidence = confidence,
                        ClassificationSource = classificationSource,
                        Active = true
                    };

                _logger.LogInformation(
                    "[MESSAGE-ADMIN-QUEUE] " +
                    "Creating queue item. " +
                    "MessageId={MessageId}; " +
                    "Category={Category}; " +
                    "Priority={Priority}; " +
                    "Confidence={Confidence}; " +
                    "ClassificationSource={ClassificationSource}",
                    created.Id,
                    category,
                    priority,
                    confidence,
                    classificationSource);

                // ----------------------------------
                // 6. Persist administrative ticket
                // ----------------------------------

                var queueId = await _adminQueueRepository.CreateAsync(queueItem, ct);

                _logger.LogInformation(
                    "[MESSAGE-ADMIN-QUEUE] " +
                    "Queue item created. " +
                    "QueueId={QueueId}; " +
                    "MessageId={MessageId}",
                    queueId,
                    created.Id);

                // ----------------------------------
                // 7. Build realtime admin DTO
                // ----------------------------------

                var adminDto =
                    new AdminMessageQueueDto
                    {
                        QueueId = queueId,
                        MessageId = created.Id,
                        Content = created.Content,
                        RelatedName = created.RelatedName,
                        SourceType = created.SourceType,
                        Latitude = created.Latitude,
                        Longitude = created.Longitude,
                        Category = category.ToString(),
                        Priority = priority,
                        Status = AdminMessageStatus.Open.ToString(),
                        Confidence = confidence,
                        ClassificationSource = classificationSource,
                        MessageCreatedAt = created.CreatedAt,
                        QueueCreatedAtUtc = DateTime.UtcNow
                    };

                // ----------------------------------
                // 8. Realtime notification to admins
                // ----------------------------------

                await _hubContext.Clients.Group("admins").SendAsync("ReceiveAdminMessageReport", adminDto, ct);

                _logger.LogInformation(
                    "[MESSAGE-ADMIN-QUEUE] " +
                    "Realtime admin notification sent. " +
                    "QueueId={QueueId}",
                    queueId);
            }

            return created;
        }


        public Task<List<UserMessage>> GetLatestAsync(int take = 100, CancellationToken ct = default)
        {
            if (take <= 0)
                take = 10;

            if (take > 500)
                take = 500;

            return _repo.GetLatestAsync(take, ct);
        }

        public Task<UserMessage?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return Task.FromResult<UserMessage?>(null);
            }

            return _repo.GetByIdAsync(id, ct);
        }


        public Task<bool> DeleteMessageAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return Task.FromResult(false);
            }

            return _repo.DeleteMessageAsync(id, ct);
        }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.