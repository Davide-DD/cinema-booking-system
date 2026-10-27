namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Seat(int rowId, int number, int totalRows, int actualRow, int totalSeats)
{
    public int RowId { get; init; } = rowId;
    public int Number { get; init; } = number;
    public double Score { get; init; } = Math.Exp(totalRows - actualRow) - Math.Exp(number - totalSeats / 2d);
}