using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Handlers.Reservations;
using APIHandler.Services.GroupReservations;
using APIHandler.Services.Locations;
using Moq;

namespace APIHandler.Tests.Handlers;

public class EditReservationHandlerTests
{
    private const string CustomerId = "customer-1";
    private const string ReservationId = "res-1";

    private readonly Mock<IDynamoDBContext> _mockDbContext = new();
    private readonly Mock<ILocationService> _mockLocationService = new();
    private readonly Mock<IGroupReservationService> _mockGroupReservationService = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly EditReservationHandler _handler;

    private readonly DateTime _newStart = DateTime.UtcNow.AddDays(2);
    private DateTime NewEnd => _newStart.AddHours(1);

    static EditReservationHandlerTests()
    {
        // The handler reads these table names in static fields, so set them before the type loads.
        Environment.SetEnvironmentVariable("reservationsTable", "Reservations");
        Environment.SetEnvironmentVariable("tablesTable", "Tables");
        Environment.SetEnvironmentVariable("reservationsTableGlobalIndex", "TableIdIndex");
    }

    public EditReservationHandlerTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILambdaLogger>().Object);
        _handler = new EditReservationHandler(
            _mockDbContext.Object,
            _mockLocationService.Object,
            _mockGroupReservationService.Object);

        ArrangeSearchResult([]);
        _mockLocationService
            .Setup(l => l.GetLocationByTableIdAsync("table-1"))
            .ReturnsAsync(new Location { Id = "loc-1", Address = "Address", Description = "d", AverageOccupancy = "Low" });
    }

    private APIGatewayProxyRequest Request(int guestCount, string sub = CustomerId) => new()
    {
        Body = $"{{\"reservationId\":\"{ReservationId}\",\"startTime\":\"{_newStart:O}\",\"endTime\":\"{NewEnd:O}\",\"guestCount\":{guestCount}}}",
        RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
        {
            Authorizer = new APIGatewayCustomAuthorizerContext
            {
                Claims = new Dictionary<string, string>
                {
                    ["sub"] = sub,
                    ["cognito:groups"] = "Customer",
                },
            },
        },
    };

    private void ArrangeReservation()
    {
        var reservation = new Reservation
        {
            Id = ReservationId,
            TableId = "table-1",
            WaiterId = "waiter-1",
            CustomerId = CustomerId,
            GuestCount = 1,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = ReservationStatus.Reserved,
        };
        _mockDbContext
            .Setup(c => c.LoadAsync<Reservation>(It.IsAny<object>(), It.IsAny<LoadConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
    }

    private void ArrangeTable(int capacity)
    {
        _mockDbContext
            .Setup(c => c.LoadAsync<Table>(It.IsAny<object>(), It.IsAny<LoadConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Table { Id = "table-1", LocationId = "loc-1", Capacity = capacity });
    }

    private void ArrangeSearchResult(List<Reservation> existing)
    {
        var search = new Mock<IAsyncSearch<Reservation>>();
        search.Setup(s => s.GetRemainingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockDbContext
            .Setup(c => c.QueryAsync<Reservation>(It.IsAny<object>(), It.IsAny<QueryConfig>()))
            .Returns(search.Object);
    }

    [Fact]
    public async Task HandleRequest_CustomerEditsGuestCountToThree_SyncsGroupWithSavedReservation()
    {
        ArrangeReservation();
        ArrangeTable(6);

        var response = await _handler.HandleRequest(Request(3), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockGroupReservationService.Verify(
            s => s.SyncGroupWithGuestCountAsync(It.Is<Reservation>(r =>
                r.Id == ReservationId
                && r.GuestCount == 3
                && r.StartTime == _newStart
                && r.EndTime == NewEnd)),
            Times.Once);
        _mockGroupReservationService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleRequest_SyncRunsAfterSave()
    {
        ArrangeReservation();
        ArrangeTable(6);
        var calls = new List<string>();
        _mockDbContext
            .Setup(c => c.SaveAsync(It.IsAny<Reservation>(), It.IsAny<SaveConfig>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save"))
            .Returns(Task.CompletedTask);
        _mockGroupReservationService
            .Setup(s => s.SyncGroupWithGuestCountAsync(It.IsAny<Reservation>()))
            .Callback(() => calls.Add("sync"))
            .Returns(Task.CompletedTask);

        await _handler.HandleRequest(Request(3), _mockContext.Object);

        Assert.Equal(["save", "sync"], calls);
    }

    [Fact]
    public async Task HandleRequest_CapacityTooSmall_Returns400AndDoesNotSync()
    {
        ArrangeReservation();
        ArrangeTable(2);

        var response = await _handler.HandleRequest(Request(3), _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        _mockGroupReservationService.Verify(s => s.SyncGroupWithGuestCountAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequest_SaveFails_Returns500AndDoesNotSync()
    {
        ArrangeReservation();
        ArrangeTable(6);
        _mockDbContext
            .Setup(c => c.SaveAsync(It.IsAny<Reservation>(), It.IsAny<SaveConfig>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var response = await _handler.HandleRequest(Request(3), _mockContext.Object);

        Assert.Equal(500, response.StatusCode);
        _mockGroupReservationService.Verify(s => s.SyncGroupWithGuestCountAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequest_CallerIsNotOwner_Returns403AndDoesNotSync()
    {
        ArrangeReservation();
        ArrangeTable(6);

        var response = await _handler.HandleRequest(Request(3, sub: "someone-else"), _mockContext.Object);

        Assert.Equal(403, response.StatusCode);
        _mockGroupReservationService.Verify(s => s.SyncGroupWithGuestCountAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequest_SyncThrows_ExceptionIsNotSwallowed()
    {
        ArrangeReservation();
        ArrangeTable(6);
        _mockGroupReservationService
            .Setup(s => s.SyncGroupWithGuestCountAsync(It.IsAny<Reservation>()))
            .ThrowsAsync(new InvalidOperationException("sync failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleRequest(Request(3), _mockContext.Object));
    }

    [Fact]
    public async Task HandleRequest_LocationLookupFails_StillSyncsBeforeTheLocationLookup()
    {
        ArrangeReservation();
        ArrangeTable(6);
        _mockLocationService
            .Setup(l => l.GetLocationByTableIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("no location"));

        var response = await _handler.HandleRequest(Request(3), _mockContext.Object);

        Assert.Equal(404, response.StatusCode);
        _mockGroupReservationService.Verify(s => s.SyncGroupWithGuestCountAsync(It.IsAny<Reservation>()), Times.Once);
    }
}
