namespace CitizenHackathon2025.Infrastructure.ExternalProviders.Common
{
    public sealed class ExternalProviderException : Exception
    {
        public string ProviderName { get; }
        public int? StatusCode { get; }

        public ExternalProviderException(
            string providerName,
            string message,
            int? statusCode = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            ProviderName = providerName;
            StatusCode = statusCode;
        }

        public override string ToString()
            => StatusCode is null
                ? $"[{ProviderName}] {Message}"
                : $"[{ProviderName}] HTTP {StatusCode} - {Message}";
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.