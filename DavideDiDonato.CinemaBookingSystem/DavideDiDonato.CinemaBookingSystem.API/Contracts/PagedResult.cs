namespace DavideDiDonato.CinemaBookingSystem.API.Contracts;

public class PagedResult<T>
{
    public T[] Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
