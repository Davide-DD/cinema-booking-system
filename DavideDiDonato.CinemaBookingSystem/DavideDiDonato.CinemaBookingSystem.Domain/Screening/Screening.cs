namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Screening
{
    public int Id { get; set; }
    public int ScreenId { get; set; }
    public int MovieId { get; set; }
    [DateTimeGreaterThanNow]
    public DateTime DateTime { get; set; }
}