using System.Net;
using System.Net.Http.Json;
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
        Assert.Single(movies);
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
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
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
    public async Task CreateOrUpdateMovie_InputExistingMovieWithNewName_UpdatesName()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
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
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
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
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
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
    public async Task CreateOrUpdateMovie_InputMismatchingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        Movie movie = (await CommonGetMovie(1))!;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies/2", movie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputNotExistingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
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
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        Movie movie = new Movie
        {
            Name = ""
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/movies", movie);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateMovie_InputPastScreening_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
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
    public async Task DeleteMovie_InputExistingId_ReturnsNoContent()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await client.DeleteAsync("/movies/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await CommonGetMovie(2));
    }

    [Fact]
    public async Task DeleteMovie_InputNotExistingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await client.DeleteAsync("/movies/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
}
