namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Seat(int rowId, int number, int totalRows, int actualRow, int totalSeats)
{
    public int RowId { get; } = rowId;
    public int Number { get; } = number;

    public double Score { get; } = Math.Exp(-Math.Abs((totalRows + 1) / 2d - actualRow) / totalRows) +
                                   Math.Exp(-Math.Abs((totalSeats + 1) / 2d - number) / totalSeats);
}