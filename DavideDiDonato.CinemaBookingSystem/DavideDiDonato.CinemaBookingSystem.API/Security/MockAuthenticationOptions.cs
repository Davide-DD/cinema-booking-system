using Microsoft.AspNetCore.Authentication;

namespace DavideDiDonato.CinemaBookingSystem.API.Security;

public class MockAuthenticationOptions: AuthenticationSchemeOptions
{
    public const string DefaultScheme = "MockAuthenticationScheme";
    public string TokenHeaderName { get; set; } = "Authorization";
}