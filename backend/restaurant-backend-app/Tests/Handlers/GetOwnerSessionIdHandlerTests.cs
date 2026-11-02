using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Handlers.GuestSessions;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using Moq;
using System.Text.Json;

namespace APIHandler.Tests.Handlers;

public class GetOwnerSessionIdHandlerTests
{
    private const string UserId = "owner-user-1";
    private const string ReservationId = "reservation-1";
    private const string OwnerSessionId = "owner-session-123";

    private readonly Mock<IGuestSessionService> _mockGuestSessionService = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly GetOwnerSessionIdHandler _handler;

    public GetOwnerSessionIdHandlerTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILambdaLogger>().Object);
        _handler = new GetOwnerSessionIdHandler(_mockGuestSessionService.Object);
    }

    private static APIGatewayProxyRequest Request(string? reservationId = ReservationId, string? sub = UserId)
    {
        var request = new APIGatewayProxyRequest
        {
            PathParameters = reservationId is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["reservationId"] = reservationId },
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

    private void ArrangeServiceReturnsOwnerSessionId()
    {
        _mockGuestSessionService
            .Setup(s => s.GetOrCreateOwnerSessionIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OwnerSessionIdResponse(OwnerSessionId));
    }

    private void ArrangeServiceThrows(Exception exception)
    {
        _mockGuestSessionService
            .Setup(s => s.GetOrCreateOwnerSessionIdAsync(UserId, ReservationId))
            .ThrowsAsync(exception);
    }

    private void VerifyServiceNeverCalled()
    {
        _mockGuestSessionService.Verify(
            s => s.GetOrCreateOwnerSessionIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static JsonElement ParseBody(APIGatewayProxyResponse response)
    {
        return JsonDocument.Parse(response.Body).RootElement;
    }

    private static void AssertError(APIGatewayProxyResponse response, int statusCode, string message)
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(message, ParseBody(response).GetProperty("error").GetString());
    }

    private static void AssertUnauthorized(APIGatewayProxyResponse response)
    {
        AssertError(response, 401, "Unauthorized.");
    }

    // Path parameter checks

    [Fact]
    public async Task HandleRequest_PathParameterMissing_Returns400WithExactBodyAndDoesNotCallService()
    {
        ArrangeServiceReturnsOwnerSessionId();

        var response = await _handler.HandleRequest(Request(reservationId: null), _mockContext.Object);

        AssertError(response, 400, "Missing 'reservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParametersNull_Returns400AndDoesNotCallService()
    {
        ArrangeServiceReturnsOwnerSessionId();
        var request = Request();
        request.PathParameters = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertError(response, 400, "Missing 'reservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParameterEmpty_Returns400AndDoesNotCallService()
    {
        ArrangeServiceReturnsOwnerSessionId();

        var response = await _handler.HandleRequest(Request(reservationId: ""), _mockContext.Object);

        AssertError(response, 400, "Missing 'reservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParameterMissingAndNoAuth_Returns400BecausePathCheckRunsFirst()
    {
        ArrangeServiceReturnsOwnerSessionId();
        var request = Request(reservationId: null);
        request.RequestContext = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        VerifyServiceNeverCalled();
    }

    // Authentication checks

    [Fact]
    public async Task HandleRequest_NoSubClaim_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturnsOwnerSessionId();

        var response = await _handler.HandleRequest(Request(sub: null), _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleRequest_SubEmptyOrWhitespace_Returns401AndDoesNotCallService(string sub)
    {
        ArrangeServiceReturnsOwnerSessionId();

        var response = await _handler.HandleRequest(Request(sub: sub), _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_RequestContextNull_Returns401WithoutException()
    {
        ArrangeServiceReturnsOwnerSessionId();
        var request = Request();
        request.RequestContext = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    // Success

    [Fact]
    public async Task HandleRequest_ValidRequest_CallsServiceWithSubAndReservationIdAndReturns200()
    {
        ArrangeServiceReturnsOwnerSessionId();

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockGuestSessionService.Verify(s => s.GetOrCreateOwnerSessionIdAsync(UserId, ReservationId), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ValidRequest_ReturnsCamelCaseGuestSessionIdBody()
    {
        ArrangeServiceReturnsOwnerSessionId();

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        var body = ParseBody(response);
        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        Assert.False(body.TryGetProperty("GuestSessionId", out _));
        Assert.Equal(OwnerSessionId, body.GetProperty("guestSessionId").GetString());
    }

    // Service exceptions

    [Fact]
    public async Task HandleRequest_ReservationNotFound_Returns404WithErrorBody()
    {
        ArrangeServiceThrows(new ReservationNotFoundException("Reservation not found."));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 404, "Reservation not found.");
    }

    [Fact]
    public async Task HandleRequest_GroupReservationNotFound_Returns404WithErrorBody()
    {
        ArrangeServiceThrows(new GroupReservationNotFoundException());

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 404, "Group reservation was not found");
    }

    [Fact]
    public async Task HandleRequest_CallerIsNotOwner_Returns403WithErrorBody()
    {
        ArrangeServiceThrows(new ActionForbiddenException("Only the reservation owner can perform this action."));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 403, "Only the reservation owner can perform this action.");
    }

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
