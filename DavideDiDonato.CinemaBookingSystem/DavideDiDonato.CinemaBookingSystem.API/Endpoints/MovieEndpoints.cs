using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
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
        RouteGroupBuilder bookingGroup = endpoints.MapGroup("/movies")
            .WithTags("Movies")
            .RequireAuthorization();

        bookingGroup.MapGet("/", GetMovies);
        bookingGroup.MapPut("/{id:int?}", CreateOrUpdateMovie).RequireAuthorization(Roles.Admin);
        bookingGroup.MapDelete("/{id:int}", DeleteMovie).RequireAuthorization(Roles.Admin);

        return endpoints;
    }

    private static async Task<NoContent> DeleteMovie([FromRoute] int id, IMovieService movieService, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await movieService.DeleteMovie(user, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<Movie>> CreateOrUpdateMovie([FromRoute] int? id, [FromBody] Movie movie, 
        IMovieService movieService, CancellationToken cancellationToken)
    { 
        return TypedResults.Ok(await movieService.CreateOrUpdateMovie(id, movie, cancellationToken));
    }

    private static async Task<Ok<Movie[]>> GetMovies([FromQuery] int page, [FromQuery] int pageSize, IMovieService movieService,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await movieService.GetMovies(user, page, pageSize, cancellationToken));
    }

}