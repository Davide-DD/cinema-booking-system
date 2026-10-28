using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.Domain;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public interface IMovieService
{
    public Task<Movie[]> GetMovies(ClaimsPrincipal currentUser, int page, int pageSize, CancellationToken cancellationToken);
    public Task DeleteMovie(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken);
    public Task<Movie> CreateOrUpdateMovie(int? id, Movie movie, CancellationToken cancellationToken);
}