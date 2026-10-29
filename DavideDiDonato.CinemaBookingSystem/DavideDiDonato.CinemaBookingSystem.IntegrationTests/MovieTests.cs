using System.Net;
using System.Net.Http.Json;
using System.Text;
using DavideDiDonato.CinemaBookingSystem.Domain;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DavideDiDonato.CinemaBookingSystem.IntegrationTests;

[Collection(CinemaBookingSystemCollection.Name)]
public class MovieTests(CinemaBookingSystemFixture cinemaBookingSystemFixture) : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await cinemaBookingSystemFixture.SeedDataAsync();
    }

    [Fact]
    public async Task GetMovies_InputFirstPage_ReturnsMoviesWithScreenings()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Movie[]? movies = await client.GetFromJsonAsync<Movie[]>("/movies?page=1&pageSize=10");

        Assert.NotNull(movies);
        Assert.Equal(2, movies.Length);
        Assert.All(movies, movie => Assert.Equal(2, movie.Screenings.Count));
    }

    [Fact]
    public async Task GetMovies_InputSecondPageOfOneMovie_ReturnsOneMovie()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Movie[]? movies = await client.GetFromJsonAsync<Movie[]>("/movies?page=2&pageSize=1");

        Assert.NotNull(movies);
        Movie movie = Assert.Single(movies);
        Assert.Equal(2, movie.Id);
    }

    [Fact]
    public async Task GetMovies_InputPageZero_ReturnsFirstPage()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Movie[]? movies = await client.GetFromJsonAsync<Movie[]>("/movies?page=0&pageSize=10");

        Assert.NotNull(movies);
        Assert.Equal(2, movies.Length);
    }

    [Fact]
    public async Task GetMovies_InputHugePage_ReturnsEmptyList()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Movie[]? movies = await client.GetFromJsonAsync<Movie[]>("/movies?page=21474838&pageSize=100");

        Assert.NotNull(movies);
        Assert.Empty(movies);
    }

    [Fact]
    public async Task GetMovies_InputNoPaging_ReturnsFirstPage()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Movie[]? movies = await client.GetFromJsonAsync<Movie[]>("/movies");

        Assert.NotNull(movies);
        Assert.Equal(2, movies.Length);
    }

    [Fact]
    public async Task GetMovies_InputNoAuthentication_ReturnsUnauthorized()
    {
        using HttpClient client = cinemaBookingSystemFixture.WebApplicationFactory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/movies?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputNewMovie_ReturnsCreatedMovie()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = new Movie
        {
            Name = "Dune",
            Screenings = new List<Screening>
            {
                new Screening
                {
                    ScreenId = 1,
                    DateTime = DateTime.UtcNow.AddDays(5)
                }
            }
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? responseMovie = await response.Content.ReadFromJsonAsync<Movie>();
        Assert.NotNull(responseMovie);
        Movie? createdMovie = await CommonGetMovie(responseMovie.Id);
        Assert.NotNull(createdMovie);
        Assert.Equal("Dune", createdMovie.Name);
        Assert.Single(createdMovie.Screenings);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputScreeningDateWithOffset_SavesUtcDate()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        // A local date is sent with its offset, like 2026-11-03T20:00:00+01:00
        DateTime localDateTime = DateTime.Today.AddDays(5).AddHours(20);
        Movie movie = new Movie
        {
            Name = "Dune",
            Screenings = new List<Screening>
            {
                new Screening
                {
                    ScreenId = 1,
                    DateTime = localDateTime
                }
            }
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? responseMovie = await response.Content.ReadFromJsonAsync<Movie>();
        Assert.NotNull(responseMovie);
        Movie? createdMovie = await CommonGetMovie(responseMovie.Id);
        Assert.NotNull(createdMovie);
        Assert.Equal(localDateTime.ToUniversalTime(), createdMovie.Screenings.Single().DateTime);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputScreeningDateWithoutTimeZone_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = new Movie
        {
            Name = "Dune",
            Screenings = new List<Screening>
            {
                new Screening
                {
                    ScreenId = 1,
                    DateTime = DateTime.SpecifyKind(DateTime.Today.AddDays(5).AddHours(20),
                        DateTimeKind.Unspecified)
                }
            }
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputExistingMovieWithNewName_UpdatesName()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Name = "The Lord of the Rings";

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? updatedMovie = await CommonGetMovie(1);
        Assert.Equal("The Lord of the Rings", updatedMovie?.Name);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputExistingMovieWithoutScreening_RemovesScreening()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Remove(movie.Screenings.Single(screening => screening.Id == 2));

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? updatedMovie = await CommonGetMovie(1);
        Assert.NotNull(updatedMovie);
        Screening remainingScreening = Assert.Single(updatedMovie.Screenings);
        Assert.Equal(1, remainingScreening.Id);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputExistingMovieWithNewScreening_AddsScreening()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Add(new Screening
        {
            ScreenId = 1,
            MovieId = 1,
            DateTime = DateTime.UtcNow.AddDays(6)
        });

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? updatedMovie = await CommonGetMovie(1);
        Assert.Equal(3, updatedMovie?.Screenings.Count);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputExistingMovieWithTwoNewScreenings_AddsBothScreenings()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        foreach (int days in new[] { 6, 7 })
            movie.Screenings.Add(new Screening
            {
                ScreenId = 1,
                MovieId = 1,
                DateTime = DateTime.UtcNow.AddDays(days)
            });

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? responseMovie = await response.Content.ReadFromJsonAsync<Movie>();
        Assert.NotNull(responseMovie);
        Assert.All(responseMovie.Screenings, screening => Assert.NotEqual(0, screening.Id));
        Movie? updatedMovie = await CommonGetMovie(1);
        Assert.Equal(4, updatedMovie?.Screenings.Count);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputScreeningsWithAnotherMovieId_KeepsScreeningsInMovie()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Single(screening => screening.Id == 1).MovieId = 2;
        movie.Screenings.Add(new Screening
        {
            ScreenId = 1,
            MovieId = 2,
            DateTime = DateTime.UtcNow.AddDays(6)
        });

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? updatedMovie = await CommonGetMovie(1);
        Movie? anotherMovie = await CommonGetMovie(2);
        Assert.Equal(3, updatedMovie?.Screenings.Count);
        Assert.Equal(2, anotherMovie?.Screenings.Count);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputScreeningOfAnotherMovie_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        Movie anotherMovie = (await CommonGetMovie(2))!;
        movie.Screenings.Add(anotherMovie.Screenings.First());

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputScreeningOnNotExistingScreen_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Add(new Screening
        {
            ScreenId = 99,
            MovieId = 1,
            DateTime = DateTime.UtcNow.AddDays(6)
        });

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputBookedScreeningOnAnotherScreen_ReturnsConflict()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        await CommonAddBooking(screeningId: 1, rowId: 1, seatNumber: 5);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Single(screening => screening.Id == 1).ScreenId = 2;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Movie? notUpdatedMovie = await CommonGetMovie(1);
        Assert.Equal(1, notUpdatedMovie?.Screenings.Single(screening => screening.Id == 1).ScreenId);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputScreeningWithoutBookingsOnAnotherScreen_MovesScreening()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Single(screening => screening.Id == 1).ScreenId = 2;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? updatedMovie = await CommonGetMovie(1);
        Assert.Equal(2, updatedMovie?.Screenings.Single(screening => screening.Id == 1).ScreenId);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputMismatchingId_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/2", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputNotExistingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = new Movie
        {
            Id = 99,
            Name = "Dune"
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/99", movie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputEmptyName_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = new Movie
        {
            Name = ""
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputBlankName_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = new Movie
        {
            Name = "   "
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputNullName_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        using StringContent content = new StringContent("{\"name\":null}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PutAsync("/movies", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputNullScreenings_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        using StringContent content = new StringContent("{\"name\":\"Dune\",\"screenings\":null}", Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await client.PutAsync("/movies", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputNullScreening_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        using StringContent content = new StringContent("{\"name\":\"Dune\",\"screenings\":[null]}", Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await client.PutAsync("/movies", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputPastScreening_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Add(new Screening
        {
            ScreenId = 1,
            MovieId = 1,
            DateTime = DateTime.UtcNow.AddDays(-1)
        });

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputScreeningMovedToPast_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Movie movie = (await CommonGetMovie(1))!;
        movie.Screenings.Single(screening => screening.Id == 1).DateTime = DateTime.UtcNow.AddDays(-1);

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputExistingMovieWithUnchangedPastScreening_UpdatesName()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        await CommonAddPastScreening();
        Movie movie = (await CommonGetMovie(1))!;
        movie.Name = "The Lord of the Rings";

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Movie? updatedMovie = await CommonGetMovie(1);
        Assert.NotNull(updatedMovie);
        Assert.Equal("The Lord of the Rings", updatedMovie.Name);
        Assert.Equal(3, updatedMovie.Screenings.Count);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputNoAdmin_ReturnsForbidden()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        Movie movie = (await CommonGetMovie(1))!;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/1", movie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMovie_InputExistingId_ReturnsNoContent()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);

        using HttpResponseMessage response = await client.DeleteAsync("/movies/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await CommonGetMovie(2));
    }

    [Fact]
    public async Task DeleteMovie_InputNotExistingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);

        using HttpResponseMessage response = await client.DeleteAsync("/movies/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMovie_InputConcurrentDeletesOfSameMovie_ReturnsOneNoContentAndNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);

        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(1, 10)
            .Select(attempt => client.DeleteAsync("/movies/2")));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.All(responses.Where(response => response.StatusCode != HttpStatusCode.NoContent),
            response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Fact]
    public async Task DeleteMovie_InputNoAdmin_ReturnsForbidden()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await client.DeleteAsync("/movies/2");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(await CommonGetMovie(2));
    }

    private async Task<Movie?> CommonGetMovie(int id)
    {
        using IServiceScope scope = cinemaBookingSystemFixture.CreateScope();
        CinemaBookingSystemDbContext cinemaBookingSystemDbContext =
            scope.ServiceProvider.GetRequiredService<CinemaBookingSystemDbContext>();

        Movie? result = await cinemaBookingSystemDbContext.Movies
            .Include(movie => movie.Screenings)
            .AsNoTracking()
            .FirstOrDefaultAsync(movie => movie.Id == id);

        return result;
    }

    private async Task<Screening> CommonAddPastScreening()
    {
        using IServiceScope scope = cinemaBookingSystemFixture.CreateScope();
        CinemaBookingSystemDbContext cinemaBookingSystemDbContext =
            scope.ServiceProvider.GetRequiredService<CinemaBookingSystemDbContext>();

        Screening result = new Screening
        {
            ScreenId = 1,
            MovieId = 1,
            DateTime = DateTime.UtcNow.AddDays(-1)
        };
        cinemaBookingSystemDbContext.Screenings.Add(result);
        await cinemaBookingSystemDbContext.SaveChangesAsync();

        return result;
    }

    private async Task CommonAddBooking(int screeningId, int rowId, int seatNumber)
    {
        using IServiceScope scope = cinemaBookingSystemFixture.CreateScope();
        CinemaBookingSystemDbContext cinemaBookingSystemDbContext =
            scope.ServiceProvider.GetRequiredService<CinemaBookingSystemDbContext>();

        cinemaBookingSystemDbContext.Bookings.Add(new Booking
        {
            UserId = 1,
            ScreeningId = screeningId,
            RowId = rowId,
            SeatNumber = seatNumber
        });
        await cinemaBookingSystemDbContext.SaveChangesAsync();
    }
}
