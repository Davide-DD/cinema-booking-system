using System.Net;
using System.Security.Claims;
using DavideDiDonato.CinemaBookingSystem.API.Contracts;
using DavideDiDonato.CinemaBookingSystem.API.Exceptions;
using DavideDiDonato.CinemaBookingSystem.Domain;
using DavideDiDonato.CinemaBookingSystem.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DavideDiDonato.CinemaBookingSystem.API.Services;

public class BookingService(CinemaBookingSystemDbContext dbContext) : IBookingService
{
    public async Task<List<Booking>> BookScreeningsAsync(ClaimsPrincipal currentUser, BookingRequest[] bookingRequests,
        CancellationToken cancellationToken)
    {
        int currentUserId = int.Parse(currentUser.FindFirst(e => e.Type == ClaimTypes.NameIdentifier)!.Value);
        List<Booking> bookings = new List<Booking>();
        foreach (IGrouping<int, BookingRequest> screeningBookingRequests in bookingRequests
                     .GroupBy(bookingRequest => bookingRequest.ScreeningId))
        {
            int screeningId = screeningBookingRequests.Key;
            Dictionary<int, HashSet<int>> bookedSeatsByRow = await dbContext.Bookings
                .Where(booking => booking.ScreeningId == screeningId)
                .GroupBy(booking => booking.RowId)
                .ToDictionaryAsync(group => group.Key, group => group.Select(booking => booking.SeatNumber).ToHashSet(),
                    cancellationToken);
            foreach (BookingRequest bookingRequest in screeningBookingRequests)
            {
                if ((bookingRequest.RowId == null && bookingRequest.SeatNumber != null) ||
                    (bookingRequest.RowId != null && bookingRequest.SeatNumber == null))
                    throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest,
                        "Cannot book by favourite row or seat");

                Booking booking = new Booking
                {
                    UserId = currentUserId,
                    ScreeningId = bookingRequest.ScreeningId
                };

                if (bookingRequest.RowId == null) // it means also the seat is null
                {
                    Screen bookingScreen =
                        await GetBookingScreenAsync(booking, cancellationToken, enforceValidity: false);
                    Seat? finalSeat = null;
                    List<Row> orderedRows = bookingScreen.Rows.OrderBy(e => e.Number).ToList();
                    // let's say that the best row is the central one and every row is horizontally centered with respect to the screen
                    int rowUpperSearchIndex = bookingScreen.Rows.Count / 2;
                    int rowLowerSearchIndex = rowUpperSearchIndex;
                    while (rowUpperSearchIndex < bookingScreen.Rows.Count || rowLowerSearchIndex >= 0)
                    {
                        Seat? candidateBestSeat = null;
                        if (rowUpperSearchIndex < bookingScreen.Rows.Count)
                        {
                            candidateBestSeat = FindFirstBestSeat(orderedRows[rowUpperSearchIndex], orderedRows,
                                rowUpperSearchIndex, bookedSeatsByRow);
                            if (finalSeat == null ||
                                (candidateBestSeat != null && candidateBestSeat.Score > finalSeat.Score))
                                finalSeat = candidateBestSeat;
                        }

                        if (rowUpperSearchIndex == rowLowerSearchIndex || rowLowerSearchIndex < 0)
                        {
                            rowUpperSearchIndex++;
                            rowLowerSearchIndex--;
                            if (finalSeat != null) break;
                            continue;
                        }

                        candidateBestSeat =
                            FindFirstBestSeat(orderedRows[rowLowerSearchIndex], orderedRows, rowLowerSearchIndex,
                                bookedSeatsByRow);

                        if (finalSeat == null ||
                            (candidateBestSeat != null && candidateBestSeat.Score > finalSeat.Score))
                            finalSeat = candidateBestSeat;

                        rowUpperSearchIndex++;
                        rowLowerSearchIndex--;
                    }

                    if (finalSeat != null)
                    {
                        booking.RowId = finalSeat.RowId;
                        booking.SeatNumber = finalSeat.Number;
                    }
                    else
                        throw new CinemaBookingSystemApiException(HttpStatusCode.Conflict,
                            "Not every booking could be confirmed");
                }
                else
                {
                    if (bookedSeatsByRow.ContainsKey(bookingRequest.RowId.Value) &&
                        bookedSeatsByRow[bookingRequest.RowId.Value].Contains(bookingRequest.SeatNumber!.Value))
                        throw new CinemaBookingSystemApiException(HttpStatusCode.Conflict,
                            "The requested seat is taken");
                    booking.RowId = bookingRequest.RowId.Value;
                    booking.SeatNumber = bookingRequest.SeatNumber!.Value;
                    await GetBookingScreenAsync(booking, cancellationToken);
                }

                HashSet<int> bookedSeats = bookedSeatsByRow.GetValueOrDefault(booking.RowId, []);
                bookedSeats.Add(booking.SeatNumber);
                bookedSeatsByRow[booking.RowId] = bookedSeats;
                bookings.Add(booking);
            }
        }

        await dbContext.Bookings.AddRangeAsync(bookings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return bookings;
    }

    private Seat? FindFirstBestSeat(Row actualRow, List<Row> orderedRows, int rowIndex,
        Dictionary<int, HashSet<int>> bookedSeatsByRow)
    {
        var seatRightSearchIndex = actualRow.Seats / 2;
        var seatLeftSearchIndex = seatRightSearchIndex;

        while (seatRightSearchIndex <= orderedRows[rowIndex].Seats &&
               seatLeftSearchIndex >= 1)
        {
            if (!bookedSeatsByRow.ContainsKey(actualRow.Id) ||
                !bookedSeatsByRow[actualRow.Id].Contains(seatRightSearchIndex))
            {
                return new Seat(actualRow.Id, seatRightSearchIndex, orderedRows.Count, rowIndex, actualRow.Seats);
            }

            if (seatRightSearchIndex == seatLeftSearchIndex)
            {
                seatRightSearchIndex++;
                seatLeftSearchIndex--;
                continue;
            }

            if (!bookedSeatsByRow.ContainsKey(actualRow.Id) ||
                !bookedSeatsByRow[actualRow.Id].Contains(seatLeftSearchIndex))
            {
                return new Seat(actualRow.Id, seatLeftSearchIndex, orderedRows.Count, rowIndex, actualRow.Seats);
            }

            seatRightSearchIndex++;
            seatLeftSearchIndex--;
        }

        return null;
    }

    private async Task<Screen> GetBookingScreenAsync(Booking? booking, CancellationToken cancellationToken,
        bool enforceValidity = true)
    {
        Screening screening = await GetBookingScreeningIfValid(booking, cancellationToken);

        Screen? screen = await dbContext.Screens.Where(e => e.Id == screening.ScreenId)
            .Include(e => e.Rows)
            .FirstOrDefaultAsync(cancellationToken);
        if (screen == null)
            throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "The screen doesn't exist");
        if (enforceValidity && screen.Rows.All(e =>
                e.Id != booking!.RowId || (e.Id == booking.RowId && e.Seats < booking.SeatNumber)))
            throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "The row or seat doesn't exist");

        return screen;
    }

    private async Task<Screening> GetBookingScreeningIfValid(Booking? booking, CancellationToken cancellationToken)
    {
        if (booking == null) throw new CinemaBookingSystemApiException(HttpStatusCode.NotFound, "Booking is null");

        Screening? screening = await dbContext.Screenings.FindAsync([booking.ScreeningId], cancellationToken);
        if (screening == null)
            throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "The screening doesn't exist");
        if (screening.DateTime.ToUniversalTime() < DateTime.UtcNow)
            throw new CinemaBookingSystemApiException(HttpStatusCode.BadRequest, "Cannot book a past screening");

        return screening;
    }

    public async Task DeleteBookingAsync(ClaimsPrincipal currentUser, int id, CancellationToken cancellationToken)
    {
        int currentUserId = int.Parse(currentUser.FindFirst(e => e.Type == ClaimTypes.NameIdentifier)!.Value);
        Booking? booking = await dbContext.Bookings.FindAsync([id], cancellationToken: cancellationToken);
        await GetBookingScreeningIfValid(booking, cancellationToken);
        if (currentUserId != booking!.UserId)
            throw new CinemaBookingSystemApiException(HttpStatusCode.Forbidden,
                "Cannot delete another user's booking"); // this should be possible for admin users
        dbContext.Bookings.Remove(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsScreeningBookingAvailableAsync(int screeningId, int rowId, int seatNumber,
        CancellationToken cancellationToken)
    {
        Booking? booking =
            await dbContext.Bookings.Where(e =>
                    e.ScreeningId == screeningId && e.RowId == rowId && e.SeatNumber == seatNumber)
                .FirstOrDefaultAsync(cancellationToken);
        if (booking != null) return false;
        booking = new Booking
        {
            RowId = rowId,
            ScreeningId = screeningId,
            SeatNumber = seatNumber
        };
        await GetBookingScreenAsync(booking, cancellationToken);
        return true;
    }
}