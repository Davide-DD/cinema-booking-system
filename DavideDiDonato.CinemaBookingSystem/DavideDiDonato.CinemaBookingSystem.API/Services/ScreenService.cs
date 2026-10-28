using System.Net;
using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.API.Exceptions;
using DavideDiDonato.CinemaBookingSystem.Domain;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public class ScreenService(CinemaBookingSystemDbContext dbContext) : IScreenService
{
    public async Task<Screen[]> GetScreens(ClaimsPrincipal currentUser, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        return await dbContext.Screens.Include(e => e.Rows)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task DeleteScreen(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken)
    {
        Screen? screen = await dbContext.Screens.FindAsync([id], cancellationToken);
        if (screen == null) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Screen doesn't exist");
        dbContext.Remove(screen);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Screen> CreateOrUpdateScreen(int? id, Screen screen, CancellationToken cancellationToken)
    {
        if (id != null && id != screen.Id) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Screen id mismatch");
        Screen? existingScreen = await dbContext.Screens.Where(e => e.Id == screen.Id)
            .Include(e => e.Rows)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingScreen == null && screen.Id != 0) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Screen doesn't exist");
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
            foreach (Row row in screen.Rows)
            {
                if (existingScreen.Rows.Any(e => e.Id == row.Id)) continue;
                await dbContext.Rows.AddAsync(row, cancellationToken);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return existingScreen;
    }
}