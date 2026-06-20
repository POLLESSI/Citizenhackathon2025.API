#if DEBUG
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/diagnostics/mongo/read")]
public sealed class MongoReadDiagnosticsController : ControllerBase
{
    private readonly IGptInteractionNoSqlRepository _gptRepository;
    private readonly ITrafficSnapshotRepository _trafficRepository;

    public MongoReadDiagnosticsController(
        IGptInteractionNoSqlRepository gptRepository,
        ITrafficSnapshotRepository trafficRepository)
    {
        _gptRepository = gptRepository;
        _trafficRepository = trafficRepository;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("mongo")]
    public async Task<IActionResult> MongoHealth(
    [FromServices] IMongoDbContext context,
    CancellationToken ct)
    {
        var result = await context.Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1),
            cancellationToken: ct);

        return Ok(new { Ok = true, Result = result.ToString() });
    }

    [HttpGet("gpt")]
    public async Task<IActionResult> GetLatestGpt(CancellationToken ct)
    {
        var items = await _gptRepository.GetLatestAsync(20, ct);
        return Ok(items);
    }

    [HttpGet("traffic")]
    public async Task<IActionResult> GetLatestTraffic(CancellationToken ct)
    {
        var items = await _trafficRepository.GetLatestAsync(20, ct);
        return Ok(items);
    }

    #if DEBUG
    [HttpPost("gpt-test")]
    public async Task<IActionResult> WriteGptTest(
        [FromServices] IGptInteractionNoSqlRepository repository,
        CancellationToken ct)
    {
        var document = new GptInteractionDocument
        {
            SqlInteractionId = null,
            PromptHash = "debug",
            PromptPreview = "Diagnostic GPT prompt",
            Response = "Diagnostic GPT response",
            Model = "debug-model",
            Success = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.InsertAsync(document, ct);

        return Ok(new
        {
            Ok = true,
            Collection = "gpt_interactions",
            InsertedId = document.Id.ToString()
        });
    }
    #endif
}
#endif





































































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.