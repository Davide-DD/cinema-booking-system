using System.ComponentModel.DataAnnotations;

namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Row
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Only positive number allowed.")]
    public int Number { get; set; }
    [Range(1, 100, ErrorMessage = "Only from 1 to 100 seats allowed.")]
    public int Seats { get; set; }
    public int ScreenId { get; set; }
}