using System.Security.Claims;
using System.Text.Encodings.Web;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.API.Security
{
    public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public DevAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "dev"),
                new Claim(ClaimTypes.Name, "dev-user@local"),
                new Claim(ClaimTypes.Email, "dev-user@local"),

                // 👇 Primary role: User (for policy "User")
                new Claim(ClaimTypes.Role, Roles.User),

                // Optional: you can also give it Admin privileges to test the Admin screens
                new Claim(ClaimTypes.Role, Roles.Admin),
            };

            var identity = new ClaimsIdentity(claims, "Dev");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Dev");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.