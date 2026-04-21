using CitizenHackathon2025.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

namespace CitizenHackathon2025.API.Tests
{
    public class MistralAIServiceTests
    {
        [Fact]
        public async Task GenerateFromPromptAsync_ReturnsValidResponse()
        {
            // Arrange
            var json = """
            {
              "message": {
                "content": "Test suggestion"
              }
            }
            """;

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:11434/")
            };

            var config = new Mock<IConfiguration>();
            config.Setup(c => c["MistralAI:Model"]).Returns("mistral");
            config.Setup(c => c["MistralAI:Temperature"]).Returns("0.3");

            var logger = new Mock<ILogger<MistralAIService>>();

            var service = new MistralAIService(
                httpClient,
                config.Object,
                logger.Object);

            // Act
            var result = await service.GenerateFromPromptAsync("Prompt de test", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test suggestion", result);
        }

        [Fact]
        public async Task StreamFromPromptAsync_ReturnsConcatenatedStream()
        {
            // Arrange
            var streamPayload =
                """
                {"message":{"content":"Bonjour "},"done":false}
                {"message":{"content":"le monde"},"done":false}
                {"message":{"content":""},"done":true}
                """;

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(streamPayload, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:11434/")
            };

            var config = new Mock<IConfiguration>();
            config.Setup(c => c["MistralAI:Model"]).Returns("mistral");
            config.Setup(c => c["MistralAI:Temperature"]).Returns("0.3");

            var logger = new Mock<ILogger<MistralAIService>>();

            var service = new MistralAIService(
                httpClient,
                config.Object,
                logger.Object);

            var receivedChunks = new List<string>();

            // Act
            var result = await service.StreamFromPromptAsync(
                "Prompt stream test",
                chunk =>
                {
                    if (!string.IsNullOrWhiteSpace(chunk))
                        receivedChunks.Add(chunk);

                    return Task.CompletedTask;
                },
                CancellationToken.None);

            // Assert
            Assert.Equal("Bonjour le monde", result);
            Assert.Equal(2, receivedChunks.Count);
            Assert.Equal("Bonjour ", receivedChunks[0]);
            Assert.Equal("le monde", receivedChunks[1]);
        }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.