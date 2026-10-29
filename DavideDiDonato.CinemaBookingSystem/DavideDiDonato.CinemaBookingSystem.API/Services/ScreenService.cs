using System.Net;
using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.API.Exceptions;
using DavideDiDonato.CinemaBookingSystem.API.Extensions;
using DavideDiDonato.CinemaBookingSystem.Domain;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public class ScreenService(CinemaBookingSystemDbContext dbContext, IBookedSeatService bookedSeatService)
    : IScreenService
{
    public async Task<Screen[]> GetScreensAsync(ClaimsPrincipal currentUser, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        return await dbContext.Screens.Include(e => e.Rows)
            .OrderBy(e => e.Id)
            .Paginate(page, pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Screen?> GetScreenAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Screens.Where(e => e.Id == id)
            .Include(e => e.Rows)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteScreenAsync(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken)
    {
        Screen? screen = await dbContext.Screens.FindAsync([id], cancellationToken);
        if (screen == null) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Screen doesn't exist");
        dbContext.Remove(screen);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Screen> CreateOrUpdateScreenAsync(int? id, Screen screen, CancellationToken cancellationToken)
    {
        if (id != null && id != screen.Id) throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "Screen id mismatch");
        // The body can still send null where the model doesn't allow it
        if (screen.Rows == null || screen.Rows.Any(e => e == null))
            throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "Rows can't be null");
        Screen? existingScreen = await dbContext.Screens.Where(e => e.Id == screen.Id)
            .Include(e => e.Rows)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingScreen == null && screen.Id != 0) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Screen doesn't exist");
        foreach (Row row in screen.Rows)
        {
            if (row.Id != 0 && (existingScreen == null || existingScreen.Rows.All(e => e.Id != row.Id)))
                throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest,
                    "The row doesn't belong to the screen");
            row.ScreenId = screen.Id;
            Row? existingRow = existingScreen?.Rows.FirstOrDefault(e => e.Id == row.Id);
            if (existingRow != null && row.Seats < existingRow.Seats &&
                await bookedSeatService.GetHighestBookedSeatNumberAsync(row.Id, cancellationToken) > row.Seats)
                throw new CinemaBookingSystemApiException(HttpStatusCode.Conflict,
                    "The row can't lose seats that are booked");
        }
        if (screen.Id == 0)
        {
            await dbContext.Screens.AddAsync(screen, cancellationToken);
            existingScreen = screen;
        }
        else
        {
            dbContext.Screens.Entry(existingScreen!).CurrentValues.SetValues(screen);
            foreach (Row row in existingScreen!.Rows)
            {
                Row? modifiedRow = screen.Rows.FirstOrDefault(e => e.Id == row.Id);
                if (modifiedRow == null) dbContext.Rows.Remove(row);
                else dbContext.Rows.Entry(row).CurrentValues.SetValues(modifiedRow);
            }
            // Added rows join the tracked ones with id 0 until saved, so new ones are told apart by their id
            foreach (Row row in screen.Rows.Where(e => e.Id == 0))
                await dbContext.Rows.AddAsync(row, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return existingScreen;
    }
}