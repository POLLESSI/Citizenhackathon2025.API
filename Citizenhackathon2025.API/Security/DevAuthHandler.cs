namespace CitizenHackathon2025.API.Security
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Options;
    using System.Security.Claims;
    using System.Text.Encodings.Web;

    public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public DevAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "dev"),
            new Claim(ClaimTypes.Name, "Developer"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Developer"),
        };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Dev"));
            var ticket = new AuthenticationTicket(principal, "Dev");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

