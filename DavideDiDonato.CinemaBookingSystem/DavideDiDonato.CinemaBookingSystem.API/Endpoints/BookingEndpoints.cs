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
        bookingGroup.MapGet("/availability", IsScreeningBookingAvailable);
        bookingGroup.MapGet("/seats", GetScreeningSeats);
        bookingGroup.MapDelete("/{id:int}", DeleteBooking);

        return endpoints;
    }

    private static async Task<NoContent> DeleteBooking([FromRoute] int id, IBookingService bookingService, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await bookingService.DeleteBookingAsync(user, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<bool>> IsScreeningBookingAvailable([FromQuery] int screeningId, [FromQuery] int rowId, [FromQuery] int seatNumber, 
        IBookingService bookingService, CancellationToken cancellationToken)
    { 
        return TypedResults.Ok(await bookingService.IsScreeningBookingAvailableAsync(screeningId, rowId, seatNumber, cancellationToken));
    }

    private static async Task<Ok<RowAvailability[]>> GetScreeningSeats([FromQuery] int screeningId,
        IBookingService bookingService, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await bookingService.GetScreeningSeatsAsync(screeningId, cancellationToken));
    }

    private static async Task<Ok<Booking[]>> GetBookings(IBookingService bookingService, ClaimsPrincipal user,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return TypedResults.Ok(await bookingService.GetBookingsAsync(user, page, pageSize, cancellationToken));
    }

    private static async Task<Ok<List<Booking>>> BookScreenings([FromBody] BookingRequest[] bookingRequests, IBookingService bookingService,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        List<Booking> result = await bookingService.BookScreeningsAsync(user, bookingRequests, cancellationToken);
        return TypedResults.Ok(result);
    }

}