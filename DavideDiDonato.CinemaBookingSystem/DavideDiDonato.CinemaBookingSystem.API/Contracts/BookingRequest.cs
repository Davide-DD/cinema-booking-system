using System.ComponentModel.DataAnnotations;

namespace DavideDiDonato.CinemaBookingSystem.API.Contracts;

public class BookingRequest
{
    public int ScreeningId { get; set; }
    public int? RowId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Only positive number allowed.")]
    public int? SeatNumber { get; set; }
}