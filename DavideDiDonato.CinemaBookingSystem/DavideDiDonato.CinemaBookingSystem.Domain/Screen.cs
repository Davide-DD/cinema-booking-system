using System.ComponentModel.DataAnnotations;

namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Screen
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Only positive number allowed.")]
    public int Number { get; set; }
    public ICollection<Row> Rows { get; set; } = new List<Row>();
}