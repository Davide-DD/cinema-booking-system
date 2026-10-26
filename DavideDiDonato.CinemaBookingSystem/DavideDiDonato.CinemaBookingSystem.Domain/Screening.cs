namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Screening
{
    public int Id { get; set; }
    public long ScreenId { get; set; }
    public long MovieId { get; set; }
    public DateTime DateTime { get; set; }
}