namespace DavideDiDonato.CinemaBookingSystem.API.Extensions;

public static class QueryableExtensions
{
    public const int MaxPageSize = 100;

    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        int boundedPage = Math.Max(page, 1);
        int boundedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        IQueryable<T> result = query
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize);

        return result;
    }
}
