using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace CitizenHackathon2025.API.Tests
{
    public class MistralAIServiceTests
    {
        [Fact]
        public async Task GenerateSuggestionAsync_ReturnsValidResponse()
        {
            // Arrange
            var mockMistralService = new Mock<IMistralAIService>();
            mockMistralService.Setup(s => s.GenerateSuggestionAsync(
                It.IsAny<string>(),
                It.IsAny<double?>(),  // latitude
                It.IsAny<double?>(),  // longitude
                It.IsAny<CancellationToken>()))
                .ReturnsAsync("Test suggestion");

            var suggestionService = new SuggestionService(
                Mock.Of<ISuggestionRepository>(),
                Mock.Of<IPlaceRepository>(),
                Mock.Of<IEventRepository>(),
                mockMistralService.Object,
                Mock.Of<IUserRepository>(),
                new MemoryCache(new MemoryCacheOptions()),
                Mock.Of<ILogger<SuggestionService>>()
            );

            // Act
            var result = await suggestionService.GenerateSuggestionAsync("context", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }
        [Fact]
        public async Task SaveSuggestionAsync_ValidSuggestion_ReturnsSavedSuggestion()
        {
            // Arrange
            var mockRepo = new Mock<ISuggestionRepository>();
            mockRepo.Setup(r => r.SaveSuggestionAsync(It.IsAny<Suggestion>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Suggestion { Id = 1, User_Id = 1 });

            var mockLogger = new Mock<ILogger<MistralAIService>>();
            var mockConfig = new Mock<IConfiguration>();
            var mockHttpClient = new Mock<HttpClient>();
            var mockCache = new Mock<IMemoryCache>();

            // Configuring IConfiguration
            mockConfig.Setup(c => c["MistralAI:ApiKey"]).Returns("test-api-key");
            mockConfig.Setup(c => c["MistralAI:ApiUrl"]).Returns("https://test-api-url.com");
            mockConfig.Setup(c => c["MistralAI:Model"]).Returns("mistral-test-model");

            var service = new MistralAIService(
                mockHttpClient.Object,
                mockConfig.Object,
                mockLogger.Object,
                Mock.Of<MistralContextBuilder>(),  
                mockRepo.Object,
                mockCache.Object  
            );

            var suggestion = new Suggestion
            {
                User_Id = 1,
                Message = "Test suggestion",
                DateSuggestion = DateTime.UtcNow
            };

            // Act
            var result = await service.SaveSuggestionAsync(suggestion);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            mockRepo.Verify(r => r.SaveSuggestionAsync(It.IsAny<Suggestion>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.