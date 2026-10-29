using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.Domain;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public interface IMovieService
{
    public Task<Movie[]> GetMoviesAsync(ClaimsPrincipal currentUser, int page, int pageSize, CancellationToken cancellationToken);
    public Task DeleteMovieAsync(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken);
    public Task<Movie> CreateOrUpdateMovieAsync(int? id, Movie movie, CancellationToken cancellationToken);
}