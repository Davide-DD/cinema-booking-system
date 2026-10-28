using DavideDiDonato.CinemaBookingSystem.Domain;

namespace DavideDiDonato.CinemaBookingSystem.UnitTest;

public class SeatTests
{
    [Fact]
    public void Score_InputsAreCentralSeatAndLeftSeat_ReturnCentralGreater()
    {
        Seat centralSeat = new Seat(1, 4, 6, 6, 7);
        Seat leftSeat = new Seat(1, 1, 6, 6, 7);
        
        Assert.True(centralSeat.Score > leftSeat.Score);
    }
    
    [Fact]
    public void Score_InputsAreCentralSeatAndRightSeat_ReturnCentralGreater()
    {
        Seat centralSeat = new Seat(1, 4, 6, 6, 7);
        Seat rightSeat = new Seat(1, 7, 6, 6, 7);
        
        Assert.True(centralSeat.Score > rightSeat.Score);
    }
    
    [Fact]
    public void Score_InputsAreCentralRowAndBottomRowSeats_ReturnCentralGreater()
    {
        Seat centralSeat = new Seat(4, 4, 7, 4, 7);
        Seat bottomSeat = new Seat(1, 4, 7, 1, 7);
        
        Assert.True(centralSeat.Score > bottomSeat.Score);
    }
    
    [Fact]
    public void Score_InputsAreCentralRowAndTopRowSeats_ReturnCentralGreater()
    {
        Seat centralSeat = new Seat(4, 4, 7, 4, 7);
        Seat topSeat = new Seat(7, 4, 7, 7, 7);
        
        Assert.True(centralSeat.Score > topSeat.Score);
    }
    
    [Fact]
    public void Score_InputsAreEquivalentSeatsButRowLengthDiffers_ReturnSameScore()
    {
        Seat firstSeat = new Seat(3, 4, 7, 3, 7);
        Seat secondSeat = new Seat(5, 5, 7, 5, 9);
        
        Assert.True(Math.Abs(firstSeat.Score - secondSeat.Score) < float.Epsilon);
    }
    
    [Fact]
    public void Score_InputsAreEquivalentRowsSeats_ReturnSameScore()
    {
        Seat firstSeat = new Seat(3, 4, 7, 3, 7);
        Seat secondSeat = new Seat(5, 4, 7, 5, 7);
        
        Assert.True(Math.Abs(firstSeat.Score - secondSeat.Score) < float.Epsilon);
    }
    
}