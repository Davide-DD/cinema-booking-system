namespace DavideDiDonato.CinemaBookingSystem.Domain;

public class Screen(ICollection<Row> rows)
{
    public int Id { get; set; }
    public int Number { get; set; }
    public ICollection<Row> Rows { get; set; } = rows;
}