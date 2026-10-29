using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
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

        screenGroup.MapPut("/{id:int?}", CreateOrUpdateScreen).RequireAuthorization(Roles.Admin);
        screenGroup.MapGet("/", GetScreens);
        screenGroup.MapDelete("/{id:int}", DeleteScreen).RequireAuthorization(Roles.Admin);

        return endpoints;
    }

    private static async Task<NoContent> DeleteScreen([FromRoute] int id, IScreenService screenService, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await screenService.DeleteScreenAsync(user, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<Screen[]>> GetScreens(ClaimsPrincipal user, IScreenService screenService,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    { 
        return TypedResults.Ok(await screenService.GetScreensAsync(user, page, pageSize, cancellationToken));
    }

    private static async Task<Ok<Screen>> CreateOrUpdateScreen([FromRoute] int? id, [FromBody] Screen screen, IScreenService screenService,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await screenService.CreateOrUpdateScreenAsync(id, screen, cancellationToken));
    }

}