using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.Domain;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public interface IScreenService
{
    public Task<Screen[]> GetScreens(ClaimsPrincipal currentUser, int page, int pageSize, CancellationToken cancellationToken);
    public Task DeleteScreen(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken);
    public Task<Screen> CreateOrUpdateScreen(int? id, Screen screen, CancellationToken cancellationToken);
}