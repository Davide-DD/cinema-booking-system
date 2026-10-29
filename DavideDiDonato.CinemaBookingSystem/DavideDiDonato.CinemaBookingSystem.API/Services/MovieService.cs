using System.Net;
using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.API.Exceptions;
using DavideDiDonato.CinemaBookingSystem.API.Extensions;
using DavideDiDonato.CinemaBookingSystem.Domain;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public class MovieService(CinemaBookingSystemDbContext dbContext, IScreenService screenService): IMovieService
{
    public async Task<Movie[]> GetMoviesAsync(ClaimsPrincipal currentUser, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await dbContext.Movies.Include(e => e.Screenings)
            .OrderBy(e => e.Id)
            .Paginate(page, pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Screening?> GetScreeningAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Screenings.FindAsync([id], cancellationToken);
    }

    public async Task DeleteMovieAsync(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken)
    {
        Movie? movie = await dbContext.Movies.FindAsync([id], cancellationToken);
        if (movie == null) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Movie doesn't exist");
        dbContext.Movies.Remove(movie);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Movie> CreateOrUpdateMovieAsync(int? id, Movie movie, CancellationToken cancellationToken)
    {
        if (id != null && id != movie.Id) throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "Movie id mismatch");
        Movie? existingMovie = await dbContext.Movies.Where(e => e.Id == movie.Id)
            .Include(e => e.Screenings)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingMovie == null && movie.Id != 0) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Movie doesn't exist");
        foreach (Screening screening in movie.Screenings)
        {
            if (screening.Id != 0 &&
                (existingMovie == null || existingMovie.Screenings.All(e => e.Id != screening.Id)))
                throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest,
                    "The screening doesn't belong to the movie");
            screening.MovieId = movie.Id;
            if (screening.DateTime.Kind == DateTimeKind.Unspecified)
                throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest,
                    "Screening dates must include a time zone");
            // The database only accepts UTC dates
            screening.DateTime = screening.DateTime.ToUniversalTime();
            if (screening.DateTime > DateTime.UtcNow) continue;
            Screening? existingScreening = existingMovie?.Screenings.FirstOrDefault(e => e.Id == screening.Id);
            if (existingScreening == null || existingScreening.DateTime != screening.DateTime)
                throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest,
                    "New or rescheduled screenings must be in the future");
        }
        foreach (int screenId in movie.Screenings.Select(e => e.ScreenId).Distinct())
        {
            Screen? screen = await screenService.GetScreenAsync(screenId, cancellationToken);
            if (screen == null)
                throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "The screen doesn't exist");
        }
        if (movie.Id == 0) await dbContext.Movies.AddAsync(movie, cancellationToken);
        else
        {
            dbContext.Movies.Entry(existingMovie!).CurrentValues.SetValues(movie);
            foreach (Screening screening in existingMovie!.Screenings)
            {
                Screening? modifiedScreening = movie.Screenings.FirstOrDefault(e => e.Id == screening.Id);
                if (modifiedScreening == null) dbContext.Screenings.Remove(screening);
                else dbContext.Screenings.Entry(screening).CurrentValues.SetValues(modifiedScreening);
            }
            foreach (Screening screening in movie.Screenings)
            {
                if (existingMovie.Screenings.Any(e => e.Id == screening.Id)) continue;
                await dbContext.Screenings.AddAsync(screening, cancellationToken);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return movie;
    }
}