using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace DavideDiDonato.CinemaBookingSystem.IntegrationTests;

[Collection(CinemaBookingSystemCollection.Name)]
public class SeederTests(CinemaBookingSystemFixture cinemaBookingSystemFixture)
{

    [Fact]
    public void SeedData_EnsureValidity()
    {
        using IServiceScope scope = cinemaBookingSystemFixture.CreateScope();
        CinemaBookingSystemDbContext cinemaBookingSystemDbContext =
            scope.ServiceProvider.GetRequiredService<CinemaBookingSystemDbContext>();
        Assert.True(cinemaBookingSystemDbContext.Movies.Count() == 2);
        Assert.True(cinemaBookingSystemDbContext.Screenings.Count() == 4);
        Assert.True(cinemaBookingSystemDbContext.Screens.Count() == 2);
        Assert.True(cinemaBookingSystemDbContext.Rows.Count() == 6);
        Assert.True(!cinemaBookingSystemDbContext.Bookings.Any());
        Assert.True(cinemaBookingSystemDbContext.Movies.Max(movie => movie.Id) == 2);
        Assert.True(cinemaBookingSystemDbContext.Screenings.Max(screening => screening.Id) == 4);
        Assert.True(cinemaBookingSystemDbContext.Screens.Max(screen => screen.Id) == 2);
        Assert.True(cinemaBookingSystemDbContext.Rows.Max(rows => rows.Id) == 6);
    }
}