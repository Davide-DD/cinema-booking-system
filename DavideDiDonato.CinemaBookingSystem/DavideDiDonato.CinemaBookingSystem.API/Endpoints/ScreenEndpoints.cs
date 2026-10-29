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
        RouteGroupBuilder bookingGroup = endpoints.MapGroup("/screens")
            .WithTags("Screens")
            .RequireAuthorization();

        bookingGroup.MapPut("/{id:int?}", CreateOrUpdateScreen).RequireAuthorization(Roles.Admin);
        bookingGroup.MapGet("/", GetScreens);
        bookingGroup.MapDelete("/{id:int}", DeleteScreen).RequireAuthorization(Roles.Admin);

        return endpoints;
    }

    private static async Task<NoContent> DeleteScreen([FromRoute] int id, IScreenService screenService, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await screenService.DeleteScreen(user, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<Screen[]>> GetScreens([FromQuery] int page, [FromQuery] int pageSize, 
        ClaimsPrincipal user, IScreenService screenService, CancellationToken cancellationToken)
    { 
        return TypedResults.Ok(await screenService.GetScreens(user, page, pageSize, cancellationToken));
    }

    private static async Task<Ok<Screen>> CreateOrUpdateScreen([FromRoute] int? id, [FromBody] Screen screen, IScreenService screenService,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await screenService.CreateOrUpdateScreen(id, screen, cancellationToken));
    }

}