using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Handlers.GroupReservations;
using APIHandler.Models.GroupReservation;
using APIHandler.Services.GroupReservations;
using Moq;
using System.Text.Json;

namespace APIHandler.Tests.Handlers;

public class SubmitGroupPreOrderHandlerTests
{
    private const string UserId = "owner-user-1";
    private const string GroupId = "group-1";
    private const string ReservationId = "reservation-1";
    private const string ValidBody = """{"reservationId": "reservation-1"}""";

    private readonly Mock<IGroupPreOrderService> _mockService = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly SubmitGroupPreOrderHandler _handler;

    public SubmitGroupPreOrderHandlerTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILambdaLogger>().Object);
        _handler = new SubmitGroupPreOrderHandler(_mockService.Object);
    }

    private static APIGatewayProxyRequest Request(string? groupReservationId = GroupId, string? sub = UserId, string? body = ValidBody)
    {
        return new APIGatewayProxyRequest
        {
            PathParameters = groupReservationId is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["groupReservationId"] = groupReservationId },
            Body = body,
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
    }

    // Service order: dish2 first, so the body order differs from a sort by id.
    private static IReadOnlyList<GroupPreOrderDishResponse> SampleResult() =>
        [new("dish2", 2), new("dish1", 3)];

    private void ArrangeServiceReturnsSample()
    {
        _mockService
            .Setup(s => s.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(SampleResult());
    }

    private void ArrangeServiceThrows(Exception exception)
    {
        _mockService
            .Setup(s => s.SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId))
            .ThrowsAsync(exception);
    }

    private void VerifyServiceNeverCalled()
    {
        _mockService.Verify(
            s => s.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static JsonElement ParseBody(APIGatewayProxyResponse response) =>
        JsonDocument.Parse(response.Body).RootElement;

    private static void AssertError(APIGatewayProxyResponse response, int statusCode, string message)
    {
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(message, ParseBody(response).GetProperty("error").GetString());
    }

    private static void AssertErrorCode(APIGatewayProxyResponse response, string message, string errorCode)
    {
        Assert.Equal(400, response.StatusCode);
        var body = ParseBody(response);
        Assert.Equal(message, body.GetProperty("message").GetString());
        Assert.Equal(errorCode, body.GetProperty("errorCode").GetString());
    }

    // Path parameter

    [Fact]
    public async Task HandleRequest_PathParameterMissing_Returns400WithExactBodyAndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(groupReservationId: null), _mockContext.Object);

        AssertError(response, 400, "Missing 'groupReservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParametersNull_Returns400AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();
        var request = Request();
        request.PathParameters = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertError(response, 400, "Missing 'groupReservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParameterEmpty_Returns400AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(groupReservationId: ""), _mockContext.Object);

        AssertError(response, 400, "Missing 'groupReservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParameterMissingAndNoAuth_Returns400BecausePathCheckRunsFirst()
    {
        ArrangeServiceReturnsSample();
        var request = Request(groupReservationId: null);
        request.RequestContext = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertError(response, 400, "Missing 'groupReservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    // Authentication

    [Fact]
    public async Task HandleRequest_NoSubClaim_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(sub: null), _mockContext.Object);

        AssertError(response, 401, "Unauthorized.");
        VerifyServiceNeverCalled();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleRequest_SubEmptyOrWhitespace_Returns401AndDoesNotCallService(string sub)
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(sub: sub), _mockContext.Object);

        AssertError(response, 401, "Unauthorized.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_RequestContextNull_Returns401WithoutException()
    {
        ArrangeServiceReturnsSample();
        var request = Request();
        request.RequestContext = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertError(response, 401, "Unauthorized.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_NoSubAndEmptyBody_Returns401BecauseAuthCheckRunsBeforeBody()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(sub: null, body: null), _mockContext.Object);

        AssertError(response, 401, "Unauthorized.");
        VerifyServiceNeverCalled();
    }

    // Body

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    public async Task HandleRequest_BodyNullOrEmpty_Returns400BodyIsEmptyAndDoesNotCallService(string? body)
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(body: body), _mockContext.Object);

        AssertError(response, 400, "Body is empty");
        VerifyServiceNeverCalled();
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"reservationId": }""")]
    public async Task HandleRequest_MalformedJson_Returns400InvalidJsonBodyAndDoesNotCallService(string body)
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(body: body), _mockContext.Object);

        AssertError(response, 400, "Invalid json body");
        VerifyServiceNeverCalled();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"reservationId": ""}""")]
    [InlineData("""{"reservationId": null}""")]
    public async Task HandleRequest_ReservationIdMissingOrEmpty_Returns400ValidationErrorsAndDoesNotCallService(string body)
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(body: body), _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        var json = ParseBody(response);
        Assert.Equal("One or more validation errors occurred.", json.GetProperty("title").GetString());
        var errors = json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("ReservationId", out var reservationIdErrors));
        var message = Assert.Single(reservationIdErrors.EnumerateArray());
        Assert.Equal("The ReservationId field is required.", message.GetString());
        VerifyServiceNeverCalled();
    }

    // Success

    [Fact]
    public async Task HandleRequest_ValidRequest_CallsServiceOnceWithSubPathIdAndBodyReservationId()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockService.Verify(s => s.SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId), Times.Once);
        _mockService.Verify(
            s => s.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ValidRequest_ReturnsCamelCaseArrayInServiceOrder()
    {
        ArrangeServiceReturnsSample();

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        var body = ParseBody(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(2, body.GetArrayLength());

        Assert.Equal("dish2", body[0].GetProperty("dishId").GetString());
        Assert.Equal(2, body[0].GetProperty("quantity").GetInt32());
        Assert.Equal("dish1", body[1].GetProperty("dishId").GetString());
        Assert.Equal(3, body[1].GetProperty("quantity").GetInt32());
        Assert.False(body[0].TryGetProperty("DishId", out _));
        Assert.False(body[0].TryGetProperty("Quantity", out _));
    }

    // Service exceptions. The 404 types and the coded 400 types derive from ValidationException,
    // so these tests fail if the handler catches ValidationException first.

    [Fact]
    public async Task HandleRequest_GroupReservationNotFound_Returns404NotValidation400()
    {
        ArrangeServiceThrows(new GroupReservationNotFoundException());

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 404, "Group reservation was not found");
    }

    [Fact]
    public async Task HandleRequest_ReservationNotFound_Returns404NotValidation400()
    {
        ArrangeServiceThrows(new ReservationNotFoundException("Reservation not found."));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 404, "Reservation not found.");
    }

    [Fact]
    public async Task HandleRequest_CallerIsNotOwner_Returns403()
    {
        ArrangeServiceThrows(new ActionForbiddenException("Only the reservation owner can perform this action."));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 403, "Only the reservation owner can perform this action.");
    }

    [Fact]
    public async Task HandleRequest_GroupReservationSubmitted_Returns400WithGroupReservationSubmittedCode()
    {
        ArrangeServiceThrows(new GroupReservationSubmittedException());

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertErrorCode(response, "Group reservation was submitted.", "GroupReservationSubmitted");
    }

    [Fact]
    public async Task HandleRequest_GroupReservationExpired_Returns400WithGroupReservationExpiredCode()
    {
        ArrangeServiceThrows(new GroupReservationExpiredException());

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertErrorCode(response, "Group reservation is expired.", "GroupReservationExpired");
    }

    [Theory]
    [InlineData("No submitted guest sessions for group reservation 'group-1'.")]
    [InlineData("Submitted guest sessions contain no dishes.")]
    [InlineData("Active pre-order for reservation 'reservation-1' not found.")]
    public async Task HandleRequest_PlainValidationException_Returns400WithUnknownCode(string message)
    {
        ArrangeServiceThrows(new ValidationException(message));

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertErrorCode(response, message, "Unknown");
    }
}
