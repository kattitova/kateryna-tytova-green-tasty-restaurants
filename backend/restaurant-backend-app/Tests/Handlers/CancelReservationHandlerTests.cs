using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Handlers.Reservations;
using APIHandler.Services.Orders;
using APIHandler.Services.Reservations;
using APIHandler.Services.Waiters;
using Moq;

namespace APIHandler.Tests.Handlers;

public class CancelReservationHandlerTests
{
    private const string CustomerId = "customer-1";
    private const string ReservationId = "res-1";

    private readonly Mock<IWaiterService> _mockWaiterService = new();
    private readonly Mock<IReservationService> _mockReservationService = new();
    private readonly Mock<IOrderService> _mockOrderService = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly CancelReservationHandler _handler;

    public CancelReservationHandlerTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILambdaLogger>().Object);
        _handler = new CancelReservationHandler(
            _mockWaiterService.Object,
            _mockReservationService.Object,
            _mockOrderService.Object);
    }

    private static APIGatewayProxyRequest Request(string sub = CustomerId, string groups = "Customer") => new()
    {
        Body = $"{{\"id\":\"{ReservationId}\"}}",
        RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
        {
            Authorizer = new APIGatewayCustomAuthorizerContext
            {
                Claims = new Dictionary<string, string>
                {
                    ["sub"] = sub,
                    ["cognito:groups"] = groups,
                },
            },
        },
    };

    private void ArrangeReservation(DateTime startTime)
    {
        var reservation = new Reservation
        {
            Id = ReservationId,
            TableId = "table-1",
            WaiterId = "waiter-1",
            CustomerId = CustomerId,
            StartTime = startTime,
            Status = ReservationStatus.Reserved,
        };

        _mockReservationService.Setup(s => s.GetReservationByIdAsync(ReservationId)).ReturnsAsync(reservation);
        _mockReservationService.Setup(s => s.UpdateReservationAsync(It.IsAny<Reservation>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task HandleRequest_ShouldCancelPreOrder_WhenReservationIsCancelled()
    {
        ArrangeReservation(DateTime.UtcNow.AddDays(1));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockReservationService.Verify(
            s => s.UpdateReservationAsync(It.Is<Reservation>(r => r.Status == ReservationStatus.Cancelled)),
            Times.Once);
        _mockOrderService.Verify(s => s.CancelPreOrderForReservationAsync(ReservationId), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ShouldCancelPreOrder_OnlyAfterReservationIsSaved()
    {
        ArrangeReservation(DateTime.UtcNow.AddDays(1));
        var calls = new List<string>();
        _mockReservationService.Setup(s => s.UpdateReservationAsync(It.IsAny<Reservation>()))
            .Callback(() => calls.Add("reservation"))
            .ReturnsAsync(true);
        _mockOrderService.Setup(s => s.CancelPreOrderForReservationAsync(ReservationId))
            .Callback(() => calls.Add("order"))
            .Returns(Task.CompletedTask);

        await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(["reservation", "order"], calls);
    }

    [Fact]
    public async Task HandleRequest_ShouldCancelPreOrder_WhenAssignedWaiterCancels()
    {
        ArrangeReservation(DateTime.UtcNow.AddDays(1));

        var response = await _handler.HandleRequest(Request(sub: "waiter-1", groups: "Waiter"), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockOrderService.Verify(s => s.CancelPreOrderForReservationAsync(ReservationId), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ShouldStillSucceed_WhenPreOrderCancelFails()
    {
        ArrangeReservation(DateTime.UtcNow.AddDays(1));
        _mockOrderService.Setup(s => s.CancelPreOrderForReservationAsync(ReservationId))
            .ThrowsAsync(new InvalidOperationException("throttled"));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockWaiterService.Verify(s => s.FindWaiterByReservationIdAsync(ReservationId), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ShouldNotCancelPreOrder_WhenTooLateToCancel()
    {
        ArrangeReservation(DateTime.UtcNow.AddMinutes(10));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        _mockOrderService.Verify(s => s.CancelPreOrderForReservationAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequest_ShouldNotCancelPreOrder_WhenCallerDoesNotOwnReservation()
    {
        ArrangeReservation(DateTime.UtcNow.AddDays(1));

        var response = await _handler.HandleRequest(Request(sub: "someone-else"), _mockContext.Object);

        Assert.Equal(403, response.StatusCode);
        _mockOrderService.Verify(s => s.CancelPreOrderForReservationAsync(It.IsAny<string>()), Times.Never);
    }
}
