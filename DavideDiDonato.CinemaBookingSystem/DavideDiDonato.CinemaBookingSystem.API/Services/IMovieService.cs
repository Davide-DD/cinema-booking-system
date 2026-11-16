using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.Domain;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public interface IMovieService
{
    public Task<PagedResult<Movie>> GetMoviesAsync(ClaimsPrincipal currentUser, int page, int pageSize,
        CancellationToken cancellationToken);
    public Task<Movie?> GetMovieAsync(int id, CancellationToken cancellationToken);
    public Task<PagedResult<Screening>> GetScreeningsAsync(int? movieId, int? screenId, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken cancellationToken);
    public Task<Screening?> GetScreeningAsync(int id, CancellationToken cancellationToken);
    public Task DeleteMovieAsync(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken);
    public Task<Movie> CreateOrUpdateMovieAsync(int? id, Movie movie, CancellationToken cancellationToken);
}