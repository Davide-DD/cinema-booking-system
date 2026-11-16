using System.Net;
using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.API.Exceptions;
using DavideDiDonato.CinemaBookingSystem.API.Security;
using DavideDiDonato.CinemaBookingSystem.API.Services;
using DavideDiDonato.CinemaBookingSystem.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DavideDiDonato.CinemaBookingSystem.API.Endpoints;

public static class MovieEndpoints
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder movieGroup = endpoints.MapGroup("/movies")
            .WithTags("Movies")
            .RequireAuthorization();

        movieGroup.MapGet("/", GetMovies);
        movieGroup.MapGet("/{id:int}", GetMovie).WithName(nameof(GetMovie));
        movieGroup.MapPost("/", CreateMovie).RequireAuthorization(Roles.Admin);
        movieGroup.MapPut("/{id:int}", UpdateMovie).RequireAuthorization(Roles.Admin);
        movieGroup.MapDelete("/{id:int}", DeleteMovie).RequireAuthorization(Roles.Admin);

        RouteGroupBuilder screeningGroup = endpoints.MapGroup("/screenings")
            .WithTags("Screenings")
            .RequireAuthorization();

        screeningGroup.MapGet("/", GetScreenings);

        return endpoints;
    }

    private static async Task<NoContent> DeleteMovie([FromRoute] int id, IMovieService movieService, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await movieService.DeleteMovieAsync(user, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<CreatedAtRoute<Movie>> CreateMovie([FromBody] Movie movie, IMovieService movieService,
        CancellationToken cancellationToken)
    {
        Movie result = await movieService.CreateOrUpdateMovieAsync(id: null, movie, cancellationToken);

        return TypedResults.CreatedAtRoute(result, nameof(GetMovie), new { id = result.Id });
    }

    private static async Task<Ok<Movie>> UpdateMovie([FromRoute] int id, [FromBody] Movie movie,
        IMovieService movieService, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await movieService.CreateOrUpdateMovieAsync(id, movie, cancellationToken));
    }

    private static async Task<Ok<Movie>> GetMovie([FromRoute] int id, IMovieService movieService,
        CancellationToken cancellationToken)
    {
        Movie? movie = await movieService.GetMovieAsync(id, cancellationToken);
        if (movie == null) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Movie doesn't exist");

        return TypedResults.Ok(movie);
    }

    private static async Task<Ok<PagedResult<Movie>>> GetMovies(IMovieService movieService, ClaimsPrincipal user,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return TypedResults.Ok(await movieService.GetMoviesAsync(user, page, pageSize, cancellationToken));
    }

    private static async Task<Ok<PagedResult<Screening>>> GetScreenings(IMovieService movieService,
        CancellationToken cancellationToken, [FromQuery] int? movieId = null, [FromQuery] int? screenId = null,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return TypedResults.Ok(await movieService.GetScreeningsAsync(movieId, screenId, from, to, page, pageSize,
            cancellationToken));
    }

}