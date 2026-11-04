using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Handlers.Reservations;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.Locations;
using APIHandler.Services.Orders;
using Moq;
using System.Text.Json;

namespace APIHandler.Tests.Handlers;

public class GetReservationsForUserHandlerTests
{
    private const string CustomerId = "customer-1";

    private readonly Mock<IReservationRepository> _mockReservationRepository = new();
    private readonly Mock<ILocationService> _mockLocationService = new();
    private readonly Mock<IOrderService> _mockOrderService = new();
    private readonly Mock<IGroupReservationRepository> _mockGroupReservationRepository = new();
    private readonly Mock<ITableRepository> _mockTableRepository = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly GetReservationsForUserHandler _handler;

    public GetReservationsForUserHandlerTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILambdaLogger>().Object);
        _handler = new GetReservationsForUserHandler(
            _mockReservationRepository.Object,
            _mockLocationService.Object,
            _mockOrderService.Object,
            _mockGroupReservationRepository.Object,
            _mockTableRepository.Object);
    }

    private static APIGatewayProxyRequest CustomerRequest() => new()
    {
        RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
        {
            Authorizer = new APIGatewayCustomAuthorizerContext
            {
                Claims = new Dictionary<string, string>
                {
                    ["sub"] = CustomerId,
                    ["cognito:groups"] = "Customer",
                },
            },
        },
    };

    private void ArrangeReservation(Table? table)
    {
        var reservation = new Reservation
        {
            Id = "res-1",
            TableId = "table-1",
            WaiterId = "waiter-1",
            CustomerId = CustomerId,
            GuestCount = 1,
            Status = ReservationStatus.Reserved,
        };

        _mockReservationRepository.Setup(r => r.GetReservationsByCustomerIdAsync(CustomerId)).ReturnsAsync([reservation]);
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync("res-1")).ReturnsAsync(reservation);
        _mockLocationService.Setup(s => s.GetLocationByTableIdAsync("table-1"))
            .ReturnsAsync(new Location
            {
                Id = "loc-1",
                Address = "48 Rustaveli Avenue",
                Description = "Flagship",
                AverageOccupancy = "50",
            });
        _mockTableRepository.Setup(r => r.GetTableByIdAsync("table-1")).ReturnsAsync(table);
    }

    private static JsonElement FirstItem(APIGatewayProxyResponse response)
    {
        using var document = JsonDocument.Parse(response.Body);
        return document.RootElement[0].Clone();
    }

    [Fact]
    public async Task HandleRequest_ShouldReturnTableNumber_ForCustomerReservation()
    {
        ArrangeReservation(new Table { Id = "table-1", LocationId = "loc-1", Capacity = 4, TableNumber = 7 });

        var response = await _handler.HandleRequest(CustomerRequest(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(7, FirstItem(response).GetProperty("tableNumber").GetInt32());
    }

    [Fact]
    public async Task HandleRequest_ShouldKeepReservationWithNullTableNumber_WhenTableReadFails()
    {
        ArrangeReservation(table: null);
        _mockTableRepository.Setup(r => r.GetTableByIdAsync("table-1")).ThrowsAsync(new InvalidOperationException("throttled"));

        var response = await _handler.HandleRequest(CustomerRequest(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, FirstItem(response).GetProperty("tableNumber").ValueKind);
    }

    [Fact]
    public async Task HandleRequest_ShouldReturnNullTableNumber_WhenTableIsMissing()
    {
        ArrangeReservation(table: null);

        var response = await _handler.HandleRequest(CustomerRequest(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(JsonValueKind.Null, FirstItem(response).GetProperty("tableNumber").ValueKind);
    }
}
