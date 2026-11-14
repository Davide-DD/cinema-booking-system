using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.Domain;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public interface IScreenService
{
    public Task<PagedResult<Screen>> GetScreensAsync(ClaimsPrincipal currentUser, int page, int pageSize,
        CancellationToken cancellationToken);
    public Task<Screen?> GetScreenAsync(int id, CancellationToken cancellationToken);
    public Task DeleteScreenAsync(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken);
    public Task<Screen> CreateOrUpdateScreenAsync(int? id, Screen screen, CancellationToken cancellationToken);
}