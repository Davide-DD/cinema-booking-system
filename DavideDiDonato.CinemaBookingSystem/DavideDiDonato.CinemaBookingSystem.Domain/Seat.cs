namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Seat(int rowId, int number, int totalRows, int actualRow, int totalSeats)
{
    public int RowId { get; } = rowId;
    public int Number { get; } = number;

    public double Score { get; } = GetRowScore(totalRows, actualRow) +
                                   Math.Exp(-Math.Abs((totalSeats + 1) / 2d - number) / totalSeats);

    // The seat part of the score is at most 1, reached by the central seat of a row with an odd number of seats
    public static double GetMaxScore(int totalRows, int actualRow)
    {
        return GetRowScore(totalRows, actualRow) + 1;
    }

    private static double GetRowScore(int totalRows, int actualRow)
    {
        return Math.Exp(-Math.Abs((totalRows + 1) / 2d - actualRow) / totalRows);
    }
}