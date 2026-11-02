using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Handlers.GuestSessions;
using APIHandler.Mappers;
using APIHandler.Models.GuestSessions;
using APIHandler.Models.Orders;
using APIHandler.Services.GuestSessions;
using Moq;
using System.Text.Json;

namespace APIHandler.Tests.Handlers;

public class GetGroupSessionsHandlerTests
{
    private const string UserId = "owner-user-1";
    private const string GroupId = "group-1";

    private readonly Mock<IGuestSessionService> _mockGuestSessionService = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly GetGroupSessionsHandler _handler;

    public GetGroupSessionsHandlerTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILambdaLogger>().Object);
        _handler = new GetGroupSessionsHandler(_mockGuestSessionService.Object);
    }

    private static APIGatewayProxyRequest Request(string? groupReservationId = GroupId, string? sub = UserId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = groupReservationId is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["groupReservationId"] = groupReservationId },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = sub is null
                        ? new Dictionary<string, string>()
                        : new Dictionary<string, string> { ["sub"] = sub }
                }
            }
        };
        return request;
    }

    private static GetAllSessionsResponse SampleResponse()
    {
        var item = new OrderItemResponse
        {
            Dish = DishMapper.ToDishResponse(new Dish { Id = "dish1", Name = "Caesar Salad", Price = 180 }),
            Quantity = 2
        };
        var participant = new Participant("session1", ParticipantType.Guest, "Andrii", [item], 360, true);
        return new GetAllSessionsResponse([participant], 360);
    }

    private void ArrangeServiceReturnsSample()
    {
        _mockGuestSessionService
            .Setup(s => s.GetGroupSessionsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(SampleResponse());
    }

    private void ArrangeServiceThrows(Exception exception)
    {
        _mockGuestSessionService
            .Setup(s => s.GetGroupSessionsAsync(UserId, GroupId))
            .ThrowsAsync(exception);
    }

    private void VerifyServiceNeverCalled()
    {
        _mockGuestSessionService.Verify(
            s => s.GetGroupSessionsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static JsonElement ParseBody(APIGatewayProxyResponse response)
    {
        return JsonDocument.Parse(response.Body).RootElement;
    }

    private static void AssertUnauthorized(APIGatewayProxyResponse response)
    {
        Assert.Equal(401, response.StatusCode);
        Assert.Equal("Unauthorized.", ParseBody(response).GetProperty("error").GetString());
    }

    // Test 1: missing path parameter

    [Fact]
    public async Task HandleRequest_PathParameterMissing_Returns400AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(groupReservationId: null), _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        Assert.True(ParseBody(response).TryGetProperty("error", out _));
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParametersNull_Returns400AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();
        var request = Request();
        request.PathParameters = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParameterEmpty_Returns400AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(groupReservationId: ""), _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParameterMissingAndNoAuth_Returns400BecausePathCheckRunsFirst()
    {
        ArrangeServiceReturnsSample();
        var request = Request(groupReservationId: null);
        request.RequestContext = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        VerifyServiceNeverCalled();
    }

    // Test 2: unauthenticated caller

    [Fact]
    public async Task HandleRequest_NoSubClaim_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(sub: null), _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_ClaimsNull_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();
        var request = Request();
        request.RequestContext.Authorizer.Claims = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleRequest_SubEmptyOrWhitespace_Returns401AndDoesNotCallService(string sub)
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(sub: sub), _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_RequestContextNull_Returns401WithoutException()
    {
        ArrangeServiceReturnsSample();
        var request = Request();
        request.RequestContext = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_AuthorizerNull_Returns401WithoutException()
    {
        ArrangeServiceReturnsSample();
        var request = Request();
        request.RequestContext.Authorizer = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    // Test 3: valid request

    [Fact]
    public async Task HandleRequest_ValidRequest_CallsServiceWithSubAndGroupIdAndReturns200()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockGuestSessionService.Verify(s => s.GetGroupSessionsAsync(UserId, GroupId), Times.Once);
    }

    // Test 4: camelCase success body

    [Fact]
    public async Task HandleRequest_ValidRequest_ReturnsCamelCaseBodyWithSessionData()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        var body = ParseBody(response);
        Assert.False(body.TryGetProperty("Participants", out _));
        Assert.Equal(360m, body.GetProperty("total").GetDecimal());

        var participants = body.GetProperty("participants");
        Assert.Equal(1, participants.GetArrayLength());
        var participant = participants[0];
        Assert.Equal("session1", participant.GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.String, participant.GetProperty("type").ValueKind);
        Assert.Equal("Guest", participant.GetProperty("type").GetString());
        Assert.Equal("Andrii", participant.GetProperty("name").GetString());
        Assert.Equal(360m, participant.GetProperty("total").GetDecimal());
        Assert.True(participant.GetProperty("isSubmitted").GetBoolean());

        var items = participant.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        var item = items[0];
        Assert.Equal("dish1", item.GetProperty("dish").GetProperty("id").GetString());
        Assert.Equal("Caesar Salad", item.GetProperty("dish").GetProperty("name").GetString());
        Assert.Equal(180m, item.GetProperty("dish").GetProperty("price").GetDecimal());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
    }

    // Tests 5 and 6: security exceptions

    [Fact]
    public async Task HandleRequest_GroupReservationNotFound_Returns404WithErrorBody()
    {
        var exception = new GroupReservationNotFoundException();
        ArrangeServiceThrows(exception);

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(404, response.StatusCode);
        Assert.Equal(exception.Message, ParseBody(response).GetProperty("error").GetString());
    }

    [Fact]
    public async Task HandleRequest_ReservationNotFound_Returns404WithErrorBody()
    {
        var exception = new ReservationNotFoundException("Reservation not found.");
        ArrangeServiceThrows(exception);

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(404, response.StatusCode);
        Assert.Equal(exception.Message, ParseBody(response).GetProperty("error").GetString());
    }

    [Fact]
    public async Task HandleRequest_CallerIsNotOwner_Returns403WithErrorBody()
    {
        var exception = new ActionForbiddenException("You are not the owner of this reservation.");
        ArrangeServiceThrows(exception);

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(403, response.StatusCode);
        Assert.Equal(exception.Message, ParseBody(response).GetProperty("error").GetString());
    }

    // Test 7: other validation errors

    [Fact]
    public async Task HandleRequest_OtherValidationException_Returns400WithErrorCode()
    {
        var exception = new GroupReservationExpiredException();
        ArrangeServiceThrows(exception);

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        var body = ParseBody(response);
        Assert.Equal("GroupReservationExpired", body.GetProperty("errorCode").GetString());
        Assert.Equal(exception.Message, body.GetProperty("message").GetString());
    }
}
