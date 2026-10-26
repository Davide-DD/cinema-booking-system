namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Movie(ICollection<Screening> screenings)
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Screening> Screenings { get; set; } = screenings;
}