using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.API.Services;
using DavideDiDonato.CinemaBookingSystem.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DavideDiDonato.CinemaBookingSystem.API.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder bookingGroup = endpoints.MapGroup("/bookings")
            .WithTags("Bookings")
            .RequireAuthorization();

        bookingGroup.MapGet("/", GetBookings);
        bookingGroup.MapPost("/", BookScreenings);
        bookingGroup.MapDelete("/{id:int}", DeleteBooking);

        // The seats of a screening are booking data, even if they are reached from the screening
        RouteGroupBuilder screeningGroup = endpoints.MapGroup("/screenings/{screeningId:int}")
            .WithTags("Screenings")
            .RequireAuthorization();

        screeningGroup.MapGet("/seats", GetScreeningSeats);
        screeningGroup.MapGet("/availability", IsScreeningBookingAvailable);

        return endpoints;
    }

    private static async Task<NoContent> DeleteBooking([FromRoute] int id, IBookingService bookingService, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await bookingService.DeleteBookingAsync(user, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<bool>> IsScreeningBookingAvailable([FromRoute] int screeningId, [FromQuery] int rowId,
        [FromQuery] int seatNumber, IBookingService bookingService, CancellationToken cancellationToken)
    { 
        return TypedResults.Ok(await bookingService.IsScreeningBookingAvailableAsync(screeningId, rowId, seatNumber, cancellationToken));
    }

    private static async Task<Ok<RowAvailability[]>> GetScreeningSeats([FromRoute] int screeningId,
        IBookingService bookingService, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await bookingService.GetScreeningSeatsAsync(screeningId, cancellationToken));
    }

    private static async Task<Ok<PagedResult<Booking>>> GetBookings(IBookingService bookingService,
        ClaimsPrincipal user, CancellationToken cancellationToken, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return TypedResults.Ok(await bookingService.GetBookingsAsync(user, page, pageSize, cancellationToken));
    }

    private static async Task<Created<List<Booking>>> BookScreenings([FromBody] BookingRequest[] bookingRequests,
        IBookingService bookingService, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        List<Booking> result = await bookingService.BookScreeningsAsync(user, bookingRequests, cancellationToken);
        // A request can create bookings of several screenings, so there is no single location to point to
        return TypedResults.Created((string?)null, result);
    }

}