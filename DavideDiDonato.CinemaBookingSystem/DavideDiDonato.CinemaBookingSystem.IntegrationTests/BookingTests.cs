using System.Net;
using System.Net.Http.Json;
using System.Text;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.Domain;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace DavideDiDonato.CinemaBookingSystem.IntegrationTests;

[Collection(CinemaBookingSystemCollection.Name)]
public class BookingTests(CinemaBookingSystemFixture cinemaBookingSystemFixture) : IAsyncLifetime
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
    public async Task BookScreenings_InputValidBooking_ReturnsBooking()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(userId: 1);

        Booking booking = await CommonBookSeat(client, screeningId: 1, rowId: 1, seatNumber: 5);

        Assert.True(booking.Id > 0);
        Assert.Equal(1, booking.UserId);
        Assert.Equal(1, booking.ScreeningId);
        Assert.Equal(1, booking.RowId);
        Assert.Equal(5, booking.SeatNumber);
    }

    [Fact]
    public async Task BookScreenings_InputNoRowAndSeat_ReturnsCentralSeat()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Booking[] bookings = await CommonBookScreenings(client, new BookingRequest
        {
            ScreeningId = 1
        });

        // Screening 1 is in screen 1: three rows of 10 seats, the central one is row 2
        Booking booking = Assert.Single(bookings);
        Assert.Equal(2, booking.RowId);
        Assert.Contains(booking.SeatNumber, new[] { 5, 6 });
    }

    [Fact]
    public async Task BookScreenings_InputNoRowAndSeatWithOddSeats_ReturnsCentralSeat()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        Booking[] bookings = await CommonBookScreenings(client, new BookingRequest
        {
            ScreeningId = 3
        });

        // Screening 3 is in screen 2, whose central row 5 has 5 seats
        Booking booking = Assert.Single(bookings);
        Assert.Equal(5, booking.RowId);
        Assert.Equal(3, booking.SeatNumber);
    }

    [Fact]
    public async Task BookScreenings_InputNoRowAndSeatWithOnlyLastSeatFree_ReturnsLastSeat()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        Screen screen = await CommonAdd(new Screen
        {
            Number = 3,
            Rows = new List<Row>
            {
                new Row
                {
                    Number = 1,
                    Seats = 2
                }
            }
        });
        Screening screening = await CommonAdd(new Screening
        {
            ScreenId = screen.Id,
            MovieId = 1,
            DateTime = DateTime.UtcNow.AddDays(1)
        });
        Row row = screen.Rows.Single();
        await CommonBookSeat(client, screening.Id, row.Id, seatNumber: 1);

        Booking[] bookings = await CommonBookScreenings(client, new BookingRequest
        {
            ScreeningId = screening.Id
        });

        Booking booking = Assert.Single(bookings);
        Assert.Equal(2, booking.SeatNumber);
    }

    [Fact]
    public async Task BookScreenings_InputNoRowAndSeatWithFullCentralRow_ReturnsSeatInAnotherRow()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        // Screening 3 is in screen 2, whose central row 5 has 5 seats
        BookingRequest[] centralRowBookingRequests = Enumerable.Range(1, 5)
            .Select(seatNumber => new BookingRequest
            {
                ScreeningId = 3,
                RowId = 5,
                SeatNumber = seatNumber
            })
            .ToArray();
        await CommonBookScreenings(client, centralRowBookingRequests);

        Booking[] bookings = await CommonBookScreenings(client, new BookingRequest
        {
            ScreeningId = 3
        });

        Booking booking = Assert.Single(bookings);
        Assert.NotEqual(5, booking.RowId);
    }

    [Fact]
    public async Task BookScreenings_InputNoRowAndSeatWithOnlyFirstRowFree_ReturnsSeatInFirstRow()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        // Screening 3 is in screen 2: row 4 has 7 seats, row 5 has 5 seats, row 6 has 3 seats
        BookingRequest[] lastRowsBookingRequests = Enumerable.Range(1, 5)
            .Select(seatNumber => new BookingRequest
            {
                ScreeningId = 3,
                RowId = 5,
                SeatNumber = seatNumber
            })
            .Concat(Enumerable.Range(1, 3)
                .Select(seatNumber => new BookingRequest
                {
                    ScreeningId = 3,
                    RowId = 6,
                    SeatNumber = seatNumber
                }))
            .ToArray();
        await CommonBookScreenings(client, lastRowsBookingRequests);

        Booking[] bookings = await CommonBookScreenings(client, new BookingRequest
        {
            ScreeningId = 3
        });

        Booking booking = Assert.Single(bookings);
        Assert.Equal(4, booking.RowId);
    }

    [Fact]
    public async Task BookScreenings_InputNoRowAndSeatWithFullCentralRowOfFourRows_ReturnsSeatInAnotherRow()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        Screen screen = await CommonAdd(new Screen
        {
            Number = 3,
            Rows = Enumerable.Range(1, 4)
                .Select(rowNumber => new Row
                {
                    Number = rowNumber,
                    Seats = 2
                })
                .ToList()
        });
        Screening screening = await CommonAdd(new Screening
        {
            ScreenId = screen.Id,
            MovieId = 1,
            DateTime = DateTime.UtcNow.AddDays(1)
        });
        // With four rows the search starts from the third one
        Row centralRow = screen.Rows.Single(row => row.Number == 3);
        BookingRequest[] centralRowBookingRequests = Enumerable.Range(1, 2)
            .Select(seatNumber => new BookingRequest
            {
                ScreeningId = screening.Id,
                RowId = centralRow.Id,
                SeatNumber = seatNumber
            })
            .ToArray();
        await CommonBookScreenings(client, centralRowBookingRequests);

        Booking[] bookings = await CommonBookScreenings(client, new BookingRequest
        {
            ScreeningId = screening.Id
        });

        Booking booking = Assert.Single(bookings);
        Assert.NotEqual(centralRow.Id, booking.RowId);
    }

    [Fact]
    public async Task BookScreenings_InputTwoBookingsWithNoRowAndSeat_ReturnsDifferentSeats()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        BookingRequest[] bookingRequests = new[]
        {
            new BookingRequest
            {
                ScreeningId = 1
            },
            new BookingRequest
            {
                ScreeningId = 1
            }
        };

        Booking[] bookings = await CommonBookScreenings(client, bookingRequests);

        Assert.Equal(2, bookings.Length);
        Assert.False(bookings[0].RowId == bookings[1].RowId && bookings[0].SeatNumber == bookings[1].SeatNumber);
    }

    [Fact]
    public async Task BookScreenings_InputSameSeatForTwoScreenings_ReturnsBothBookings()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        // Screenings 1 and 2 are both in screen 1
        BookingRequest[] bookingRequests = new[]
        {
            new BookingRequest
            {
                ScreeningId = 1,
                RowId = 1,
                SeatNumber = 5
            },
            new BookingRequest
            {
                ScreeningId = 2,
                RowId = 1,
                SeatNumber = 5
            }
        };

        Booking[] bookings = await CommonBookScreenings(client, bookingRequests);

        Assert.Equal(2, bookings.Length);
    }

    [Fact]
    public async Task BookScreenings_InputAlreadyBookedSeat_ReturnsConflict()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(userId: 1);
        using HttpClient anotherUserClient = cinemaBookingSystemFixture.CreateAuthenticatedClient(userId: 2);
        await CommonBookSeat(client, screeningId: 1, rowId: 1, seatNumber: 5);

        using HttpResponseMessage response =
            await CommonPostBooking(anotherUserClient, screeningId: 1, rowId: 1, seatNumber: 5);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task BookScreenings_InputConcurrentBookingsOfSameSeat_ReturnsOneBookingAndConflicts()
    {
        HttpClient[] clients = Enumerable.Range(1, 10)
            .Select(userId => cinemaBookingSystemFixture.CreateAuthenticatedClient(userId))
            .ToArray();

        HttpResponseMessage[] responses = await Task.WhenAll(clients
            .Select(client => CommonPostBooking(client, screeningId: 1, rowId: 1, seatNumber: 5)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(response => response.StatusCode != HttpStatusCode.OK),
            response => Assert.Equal(HttpStatusCode.Conflict, response.StatusCode));
    }

    [Fact]
    public async Task BookScreenings_InputConcurrentBookingsWithNoRowAndSeat_ReturnsDifferentSeats()
    {
        HttpClient[] clients = Enumerable.Range(1, 10)
            .Select(userId => cinemaBookingSystemFixture.CreateAuthenticatedClient(userId))
            .ToArray();

        Booking[][] bookings = await Task.WhenAll(clients
            .Select(client => CommonBookScreenings(client, new BookingRequest
            {
                ScreeningId = 1
            })));

        Assert.Equal(10, bookings
            .Select(clientBookings => Assert.Single(clientBookings))
            .Select(booking => (booking.RowId, booking.SeatNumber))
            .Distinct()
            .Count());
    }

    [Fact]
    public async Task BookScreenings_InputOnlyRow_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await CommonPostBooking(client, screeningId: 1, rowId: 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BookScreenings_InputScreeningIdAsText_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        using StringContent content = new StringContent("[{\"screeningId\":\"1\"}]", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync("/bookings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BookScreenings_InputNotExistingSeat_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await CommonPostBooking(client, screeningId: 1, rowId: 1, seatNumber: 11);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BookScreenings_InputRowOfAnotherScreen_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await CommonPostBooking(client, screeningId: 1, rowId: 4, seatNumber: 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BookScreenings_InputNotExistingScreening_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await CommonPostBooking(client, screeningId: 99, rowId: 1, seatNumber: 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BookScreenings_InputPastScreening_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        Screening pastScreening = await CommonAddPastScreening();

        using HttpResponseMessage response = await CommonPostBooking(client, pastScreening.Id, rowId: 1, seatNumber: 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BookScreenings_InputNoAuthentication_ReturnsUnauthorized()
    {
        using HttpClient client = cinemaBookingSystemFixture.WebApplicationFactory.CreateClient();

        using HttpResponseMessage response = await CommonPostBooking(client, screeningId: 1, rowId: 1, seatNumber: 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IsScreeningBookingAvailable_InputFreeSeat_ReturnsTrue()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        bool isAvailable = await client.GetFromJsonAsync<bool>("/bookings?screeningId=1&rowId=1&seatNumber=5");

        Assert.True(isAvailable);
    }

    [Fact]
    public async Task IsScreeningBookingAvailable_InputBookedSeat_ReturnsFalse()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();
        await CommonBookSeat(client, screeningId: 1, rowId: 1, seatNumber: 5);

        bool isAvailable = await client.GetFromJsonAsync<bool>("/bookings?screeningId=1&rowId=1&seatNumber=5");

        Assert.False(isAvailable);
    }

    [Fact]
    public async Task IsScreeningBookingAvailable_InputNotExistingSeat_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await client.GetAsync("/bookings?screeningId=1&rowId=1&seatNumber=11");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBooking_InputExistingId_ReturnsNoContent()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(userId: 1);
        Booking booking = await CommonBookSeat(client, screeningId: 1, rowId: 1, seatNumber: 5);

        using HttpResponseMessage response = await client.DeleteAsync($"/bookings/{booking.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(await client.GetFromJsonAsync<bool>("/bookings?screeningId=1&rowId=1&seatNumber=5"));
    }

    [Fact]
    public async Task DeleteBooking_InputAnotherUserBooking_ReturnsForbidden()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(userId: 1);
        using HttpClient anotherUserClient = cinemaBookingSystemFixture.CreateAuthenticatedClient(userId: 2);
        Booking booking = await CommonBookSeat(client, screeningId: 1, rowId: 1, seatNumber: 5);

        using HttpResponseMessage response = await anotherUserClient.DeleteAsync($"/bookings/{booking.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBooking_InputNotExistingId_ReturnsNotFound()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient();

        using HttpResponseMessage response = await client.DeleteAsync("/bookings/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBooking_InputPastScreeningBooking_ReturnsBadRequest()
    {
        using HttpClient client = cinemaBookingSystemFixture.CreateAuthenticatedClient(userId: 1);
        Screening pastScreening = await CommonAddPastScreening();
        Booking booking = await CommonAdd(new Booking
        {
            UserId = 1,
            ScreeningId = pastScreening.Id,
            RowId = 1,
            SeatNumber = 5
        });

        using HttpResponseMessage response = await client.DeleteAsync($"/bookings/{booking.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Booking[]> CommonBookScreenings(HttpClient client, params BookingRequest[] bookingRequests)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/bookings", bookingRequests);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Booking[] result = (await response.Content.ReadFromJsonAsync<Booking[]>())!;

        return result;
    }

    private async Task<Booking> CommonBookSeat(HttpClient client, int screeningId, int rowId, int seatNumber)
    {
        Booking[] bookings = await CommonBookScreenings(client, new BookingRequest
        {
            ScreeningId = screeningId,
            RowId = rowId,
            SeatNumber = seatNumber
        });

        Booking result = Assert.Single(bookings);

        return result;
    }

    private async Task<HttpResponseMessage> CommonPostBooking(HttpClient client, int screeningId, int? rowId = null,
        int? seatNumber = null)
    {
        HttpResponseMessage result = await client.PostAsJsonAsync("/bookings", new[]
        {
            new BookingRequest
            {
                ScreeningId = screeningId,
                RowId = rowId,
                SeatNumber = seatNumber
            }
        });

        return result;
    }

    private async Task<Screening> CommonAddPastScreening()
    {
        Screening result = await CommonAdd(new Screening
        {
            ScreenId = 1,
            MovieId = 1,
            DateTime = DateTime.UtcNow.AddDays(-1)
        });

        return result;
    }

    private async Task<T> CommonAdd<T>(T entity) where T : class
    {
        using IServiceScope scope = cinemaBookingSystemFixture.CreateScope();
        CinemaBookingSystemDbContext cinemaBookingSystemDbContext =
            scope.ServiceProvider.GetRequiredService<CinemaBookingSystemDbContext>();

        cinemaBookingSystemDbContext.Add(entity);
        await cinemaBookingSystemDbContext.SaveChangesAsync();

        return entity;
    }
}
