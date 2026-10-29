using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DavideDiDonato.CinemaBookingSystem.API.Security;

public class MockAuthenticationHandler(
    IOptionsMonitor<MockAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<MockAuthenticationOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";
    private const char RoleSeparator = ':';

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? authorizationHeader = Request.Headers[Options.TokenHeaderName];
        if (authorizationHeader == null ||
            !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());
        
        string[] tokenParts = authorizationHeader.Substring(BearerPrefix.Length).Trim().Split(RoleSeparator);
        string userId = tokenParts[0];
        if (userId.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        if (tokenParts.Length > 1) claims.Add(new Claim(ClaimTypes.Role, tokenParts[1]));

        var claimsIdentity = new ClaimsIdentity(claims, this.Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, 
            this.Scheme.Name)));
    }
}