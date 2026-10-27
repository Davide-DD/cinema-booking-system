using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.Domain;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public interface IBookingService
{
    public Task<List<Booking>> BookScreeningsAsync(ClaimsPrincipal currentUser, BookingRequest[] bookingRequests, CancellationToken cancellationToken);
    public Task DeleteBookingAsync(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken);
    public Task<bool> IsScreeningBookingAvailableAsync(int screeningId, int rowId, int seatNumber, CancellationToken cancellationToken);
}