using DavideDiDonato.CinemaBookingSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;

public class CinemaBookingSystemDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Movie>(e =>
        {
            e.HasKey(movie => movie.Id);

            e.HasMany(movie => movie.Screenings).WithOne().HasForeignKey(screening => screening.MovieId);
        });

        modelBuilder.Entity<Screen>(e =>
        {
            e.HasKey(screen => screen.Id);

            e.HasMany(screen => screen.Rows).WithOne().HasForeignKey(row => row.ScreenId);
        });
        
        modelBuilder.Entity<Row>(e =>
        {
            e.HasKey(row => row.Id);

            e.HasMany<Booking>().WithOne().HasForeignKey(booking => booking.RowId);
        });
        
        modelBuilder.Entity<Screening>(e =>
        {
            e.HasKey(screening => screening.Id);

            e.HasMany<Booking>().WithOne().HasForeignKey(booking => booking.ScreeningId);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasKey(booking => new {
                booking.ScreeningId, booking.RowId, booking.SeatNumber, booking.UserId
            });
        });
    }
}