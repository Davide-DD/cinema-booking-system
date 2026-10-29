using System.ComponentModel.DataAnnotations;

namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Movie
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
}