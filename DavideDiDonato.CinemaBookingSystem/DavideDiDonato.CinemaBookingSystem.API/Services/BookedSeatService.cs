using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

// What the programming and venue domains can ask about bookings. It is not part of BookingService, which already
// depends on their services
public class BookedSeatService(CinemaBookingSystemDbContext dbContext) : IBookedSeatService
{
    public async Task<bool> HasBookedSeatsAsync(int screeningId, CancellationToken cancellationToken)
    {
        return await dbContext.Bookings.AnyAsync(e => e.ScreeningId == screeningId, cancellationToken);
    }

    public async Task<int?> GetHighestBookedSeatNumberAsync(int rowId, CancellationToken cancellationToken)
    {
        return await dbContext.Bookings.Where(e => e.RowId == rowId)
            .MaxAsync(e => (int?)e.SeatNumber, cancellationToken);
    }
}
