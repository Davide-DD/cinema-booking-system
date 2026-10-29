namespace DavideDiDonato.CinemaBookingSystem.API.Extensions;

public static class QueryableExtensions
{
    public const int MaxPageSize = 100;

    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        int boundedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        // Keeps the records to skip within int, so a huge page returns an empty list instead of overflowing
        int boundedPage = Math.Clamp(page, 1, int.MaxValue / boundedPageSize);

        IQueryable<T> result = query
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize);

        return result;
    }
}
