using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Mappers;

namespace APIHandler.Tests.Helpers;

public class ReservationMapperTests
{
    private static Reservation SampleReservation() => new()
    {
        Id = "res-1",
        TableId = "table-1",
        WaiterId = "waiter-1",
        Status = ReservationStatus.Reserved,
    };

    [Fact]
    public void ToReservationResponse_ShouldMapTableNumber_WhenGiven()
    {
        var response = ReservationMapper.ToReservationResponse(SampleReservation(), "48 Rustaveli Avenue", tableNumber: 7);

        Assert.Equal(7, response.TableNumber);
    }

    [Fact]
    public void ToReservationResponse_ShouldLeaveTableNumberNull_WhenNotGiven()
    {
        var response = ReservationMapper.ToReservationResponse(SampleReservation(), "48 Rustaveli Avenue");

        Assert.Null(response.TableNumber);
    }
}
