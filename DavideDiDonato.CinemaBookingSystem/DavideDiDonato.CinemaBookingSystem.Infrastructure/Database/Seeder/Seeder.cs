using DavideDiDonato.CinemaBookingSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.Infrastructure.Database.Seeder;

public class Seeder(CinemaBookingSystemDbContext dbContext)
{
    public async Task SeedData(CancellationToken? cancellationToken = null)
    {
        await dbContext.Database.MigrateAsync(cancellationToken ?? CancellationToken.None);
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"Bookings\", \"Rows\", \"Screenings\", \"Screens\", \"Movies\" RESTART IDENTITY CASCADE",
            cancellationToken ?? CancellationToken.None);

        Screen[] screens = new Screen[]
        {
            new Screen
            {
                Id = 1,
                Number = 1,
                Rows = new List<Row>()
                {
                    new Row
                    {
                        Id = 1,
                        Number = 1,
                        Seats = 10
                    },
                    new Row
                    {
                        Id = 2,
                        Number = 2,
                        Seats = 10
                    },
                    new Row
                    {
                        Id = 3,
                        Number = 3,
                        Seats = 10
                    }
                }
            },
            new Screen
            {
                Id = 2,
                Number = 2,
                Rows = new List<Row>
                {
                    new Row
                    {
                        Id = 4,
                        Number = 1,
                        Seats = 7
                    },
                    new Row
                    {
                        Id = 5,
                        Number = 2,
                        Seats = 5
                    },
                    new Row
                    {
                        Id = 6,
                        Number = 3,
                        Seats = 3
                    }
                }
            }
        };
        dbContext.Screens.AddRange(screens);
        
        Movie[] movies = new Movie[]
        {
           new Movie
           {
               Id = 1,
               Name = "Lord of the Rings",
               Screenings = new List<Screening>()
               {
                   new Screening
                   {
                       Id = 1,
                       ScreenId = 1,
                       DateTime = DateTime.UtcNow.AddDays(1)
                   },
                   new Screening
                   {
                       Id = 2,
                       ScreenId = 1,
                       DateTime = DateTime.UtcNow.AddDays(3)
                   }
               }
           },
           new Movie
           {
               Id = 2,
               Name = "Matrix",
               Screenings = new List<Screening>()
               {
                   new Screening
                   {
                       Id = 3,
                       ScreenId = 2,
                       DateTime = DateTime.UtcNow.AddDays(2)
                   },
                   new Screening
                   {
                       Id = 4,
                       ScreenId = 2,
                       DateTime = DateTime.UtcNow.AddDays(4)
                   }
               }
           }
        };
        dbContext.Movies.AddRange(movies);

        await dbContext.SaveChangesAsync(cancellationToken ?? CancellationToken.None);

        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT setval(pg_get_serial_sequence('\"Movies\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Movies\"));" +
            "SELECT setval(pg_get_serial_sequence('\"Rows\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Rows\"));" +
            "SELECT setval(pg_get_serial_sequence('\"Screens\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Screens\"));" +
            "SELECT setval(pg_get_serial_sequence('\"Screenings\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Screenings\"))",
            cancellationToken ?? CancellationToken.None);
    }
}