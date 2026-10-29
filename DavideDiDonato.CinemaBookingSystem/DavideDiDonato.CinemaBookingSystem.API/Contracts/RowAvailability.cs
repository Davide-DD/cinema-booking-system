namespace DavideDiDonato.CinemaBookingSystem.API.Contracts;

public class RowAvailability
{
    public int RowId { get; set; }
    public int Number { get; set; }
    public int Seats { get; set; }
    public int[] BookedSeats { get; set; } = Array.Empty<int>();
}
