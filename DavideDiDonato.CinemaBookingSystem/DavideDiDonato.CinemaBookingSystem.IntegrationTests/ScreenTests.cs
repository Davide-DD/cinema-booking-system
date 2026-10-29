using System.Net;
using System.Net.Http.Json;
using DavideDiDonato.CinemaBookingSystem.Domain;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DavideDiDonato.CinemaBookingSystem.IntegrationTests;

[Collection(CinemaBookingSystemCollection.Name)]
public class ScreenTests(CinemaBookingSystemFixture cinemaBookingSystemFixture) : IAsyncLifetime
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
    public async Task GetScreens_InputFirstPage_ReturnsScreensWithRows()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Screen[]? screens = await client.GetFromJsonAsync<Screen[]>("/screens?page=1&pageSize=10");

        Assert.NotNull(screens);
        Assert.Equal(2, screens.Length);
        Assert.All(screens, screen => Assert.Equal(3, screen.Rows.Count));
    }

    [Fact]
    public async Task GetScreens_InputSecondPageOfOneScreen_ReturnsOneScreen()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Screen[]? screens = await client.GetFromJsonAsync<Screen[]>("/screens?page=2&pageSize=1");

        Assert.NotNull(screens);
        Screen screen = Assert.Single(screens);
        Assert.Equal(2, screen.Id);
    }

    [Fact]
    public async Task GetScreens_InputPageZero_ReturnsFirstPage()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Screen[]? screens = await client.GetFromJsonAsync<Screen[]>("/screens?page=0&pageSize=10");

        Assert.NotNull(screens);
        Assert.Equal(2, screens.Length);
    }

    [Fact]
    public async Task GetScreens_InputNoAuthentication_ReturnsUnauthorized()
    {
        using HttpClient client = cinemaBookingSystemFixture.WebApplicationFactory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/screens?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputNewScreen_ReturnsCreatedScreen()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Screen screen = new Screen
        {
            Number = 3,
            Rows = new List<Row>
            {
                new Row
                {
                    Number = 1,
                    Seats = 8
                },
                new Row
                {
                    Number = 2,
                    Seats = 8
                }
            }
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens", screen);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Screen? responseScreen = await response.Content.ReadFromJsonAsync<Screen>();
        Assert.NotNull(responseScreen);
        Screen? createdScreen = await CommonGetScreen(responseScreen.Id);
        Assert.NotNull(createdScreen);
        Assert.Equal(3, createdScreen.Number);
        Assert.Equal(2, createdScreen.Rows.Count);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputExistingScreenWithNewNumber_UpdatesNumber()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Screen screen = (await CommonGetScreen(1))!;
        screen.Number = 3;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens/1", screen);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Screen? updatedScreen = await CommonGetScreen(1);
        Assert.Equal(3, updatedScreen?.Number);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputExistingScreenWithoutRow_RemovesRow()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Screen screen = (await CommonGetScreen(1))!;
        screen.Rows.Remove(screen.Rows.Single(row => row.Id == 3));

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens/1", screen);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Screen? updatedScreen = await CommonGetScreen(1);
        Assert.NotNull(updatedScreen);
        Assert.Equal(2, updatedScreen.Rows.Count);
        Assert.DoesNotContain(updatedScreen.Rows, row => row.Id == 3);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputExistingScreenWithNewRow_AddsRow()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Screen screen = (await CommonGetScreen(1))!;
        screen.Rows.Add(new Row
        {
            Number = 4,
            Seats = 10,
            ScreenId = 1
        });

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens/1", screen);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Screen? updatedScreen = await CommonGetScreen(1);
        Assert.Equal(4, updatedScreen?.Rows.Count);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputMismatchingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Screen screen = (await CommonGetScreen(1))!;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens/2", screen);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputNotExistingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Screen screen = new Screen
        {
            Id = 99,
            Number = 99
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens/99", screen);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputRowWithoutSeats_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);
        Screen screen = (await CommonGetScreen(1))!;
        screen.Rows.Add(new Row
        {
            Number = 4,
            Seats = 0,
            ScreenId = 1
        });

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens/1", screen);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateScreen_InputNoAdmin_ReturnsForbidden()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        Screen screen = (await CommonGetScreen(1))!;

        using HttpResponseMessage response = await client.PutAsJsonAsync("/screens/1", screen);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteScreen_InputExistingId_ReturnsNoContent()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);

        using HttpResponseMessage response = await client.DeleteAsync("/screens/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await CommonGetScreen(2));
    }

    [Fact]
    public async Task DeleteScreen_InputNotExistingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(asAdmin: true);

        using HttpResponseMessage response = await client.DeleteAsync("/screens/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteScreen_InputNoAdmin_ReturnsForbidden()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await client.DeleteAsync("/screens/2");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(await CommonGetScreen(2));
    }

    private async Task<Screen?> CommonGetScreen(int id)
    {
        using IServiceScope scope = cinemaBookingSystemFixture.CreateScope();
        CinemaBookingSystemDbContext cinemaBookingSystemDbContext =
            scope.ServiceProvider.GetRequiredService<CinemaBookingSystemDbContext>();

        Screen? result = await cinemaBookingSystemDbContext.Screens
            .Include(screen => screen.Rows)
            .AsNoTracking()
            .FirstOrDefaultAsync(screen => screen.Id == id);

        return result;
    }
}
