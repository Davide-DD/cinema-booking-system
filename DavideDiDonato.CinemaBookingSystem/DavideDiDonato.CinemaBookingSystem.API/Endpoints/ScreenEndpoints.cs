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

public static class ScreenEndpoints
{
    public static IEndpointRouteBuilder MapScreenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder screenGroup = endpoints.MapGroup("/screens")
            .WithTags("Screens")
            .RequireAuthorization();

        screenGroup.MapPost("/", CreateScreen).RequireAuthorization(Roles.Admin);
        screenGroup.MapPut("/{id:int}", UpdateScreen).RequireAuthorization(Roles.Admin);
        screenGroup.MapGet("/", GetScreens);
        screenGroup.MapGet("/{id:int}", GetScreen).WithName(nameof(GetScreen));
        screenGroup.MapDelete("/{id:int}", DeleteScreen).RequireAuthorization(Roles.Admin);

        return endpoints;
    }

    private static async Task<NoContent> DeleteScreen([FromRoute] int id, IScreenService screenService, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await screenService.DeleteScreenAsync(user, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<Screen>>> GetScreens(ClaimsPrincipal user, IScreenService screenService,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    { 
        return TypedResults.Ok(await screenService.GetScreensAsync(user, page, pageSize, cancellationToken));
    }

    private static async Task<Ok<Screen>> GetScreen([FromRoute] int id, IScreenService screenService,
        CancellationToken cancellationToken)
    {
        Screen? screen = await screenService.GetScreenAsync(id, cancellationToken);
        if (screen == null) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Screen doesn't exist");

        return TypedResults.Ok(screen);
    }

    private static async Task<CreatedAtRoute<Screen>> CreateScreen([FromBody] Screen screen,
        IScreenService screenService, CancellationToken cancellationToken)
    {
        Screen result = await screenService.CreateOrUpdateScreenAsync(id: null, screen, cancellationToken);

        return TypedResults.CreatedAtRoute(result, nameof(GetScreen), new { id = result.Id });
    }

    private static async Task<Ok<Screen>> UpdateScreen([FromRoute] int id, [FromBody] Screen screen,
        IScreenService screenService, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await screenService.CreateOrUpdateScreenAsync(id, screen, cancellationToken));
    }

}