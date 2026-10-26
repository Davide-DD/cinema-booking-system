namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Booking
{
    public long UserId { get; set; }
    public long ScreeningId { get; set; }
    public long RowId { get; set; }
    public int SeatNumber { get; set; }
}