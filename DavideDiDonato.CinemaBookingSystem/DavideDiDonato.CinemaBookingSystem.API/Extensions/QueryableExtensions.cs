using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.API.Extensions;

public static class QueryableExtensions
{
    public const int MaxPageSize = 100;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        int boundedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        // Keeps the records to skip within int, so a huge page returns an empty list instead of overflowing
        int boundedPage = Math.Clamp(page, 1, int.MaxValue / boundedPageSize);

        PagedResult<T> result = new PagedResult<T>
        {
            Items = await query
                .Skip((boundedPage - 1) * boundedPageSize)
                .Take(boundedPageSize)
                .ToArrayAsync(cancellationToken),
            Page = boundedPage,
            PageSize = boundedPageSize,
            TotalCount = await query.CountAsync(cancellationToken)
        };

        return result;
    }
}
