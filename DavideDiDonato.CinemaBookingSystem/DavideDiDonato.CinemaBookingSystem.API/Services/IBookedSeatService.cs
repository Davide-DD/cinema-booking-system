namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public interface IBookedSeatService
{
    public Task<bool> HasBookedSeatsAsync(int screeningId, CancellationToken cancellationToken);
    public Task<int?> GetHighestBookedSeatNumberAsync(int rowId, CancellationToken cancellationToken);
}
