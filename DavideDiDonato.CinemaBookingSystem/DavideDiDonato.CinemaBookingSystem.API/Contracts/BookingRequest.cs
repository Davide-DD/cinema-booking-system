namespace DavideDiDonato.CinemaBookingSystem.API.Contracts;

public class BookingRequest
{
    public int ScreeningId { get; set; }
    public int? RowId { get; set; }
    public int? SeatNumber { get; set; }
}