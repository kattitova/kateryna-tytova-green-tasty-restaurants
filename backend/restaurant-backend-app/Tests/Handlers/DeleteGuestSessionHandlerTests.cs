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

public class DeleteGuestSessionHandlerTests
{
    private const string UserId = "owner-user-1";
    private const string GroupId = "group-1";
    private const string SessionId = "session1";

    private readonly Mock<IGuestSessionService> _mockGuestSessionService = new();
    private readonly Mock<ILambdaContext> _mockContext = new();
    private readonly DeleteGuestSessionHandler _handler;

    public DeleteGuestSessionHandlerTests()
    {
        _mockContext.Setup(c => c.Logger).Returns(new Mock<ILambdaLogger>().Object);
        _handler = new DeleteGuestSessionHandler(_mockGuestSessionService.Object);
    }

    private static APIGatewayProxyRequest Request(
        string? groupReservationId = GroupId, string? guestSessionId = SessionId, string? sub = UserId)
    {
        var pathParameters = new Dictionary<string, string>();
        if (groupReservationId is not null)
        {
            pathParameters["groupReservationId"] = groupReservationId;
        }
        if (guestSessionId is not null)
        {
            pathParameters["guestSessionId"] = guestSessionId;
        }

        return new APIGatewayProxyRequest
        {
            PathParameters = pathParameters,
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

    private static GetAllSessionsResponse SampleResponse()
    {
        var item = new OrderItemResponse
        {
            Dish = DishMapper.ToDishResponse(new Dish { Id = "dish2", Name = "Tom Yum", Price = 220 }),
            Quantity = 1
        };
        var participant = new Participant("session2", ParticipantType.Guest, "Olena", [item], 220, false);
        return new GetAllSessionsResponse([participant], 220);
    }

    private void ArrangeServiceReturns(GetAllSessionsResponse response)
    {
        _mockGuestSessionService
            .Setup(s => s.DeleteGuestSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);
    }

    private void ArrangeServiceThrows(Exception exception)
    {
        _mockGuestSessionService
            .Setup(s => s.DeleteGuestSessionAsync(UserId, GroupId, SessionId))
            .ThrowsAsync(exception);
    }

    private void VerifyServiceNeverCalled()
    {
        _mockGuestSessionService.Verify(
            s => s.DeleteGuestSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

    // Path parameters

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleRequest_GroupReservationIdMissingOrEmpty_Returns400AndDoesNotCallService(string? groupReservationId)
    {
        ArrangeServiceReturns(SampleResponse());

        var response = await _handler.HandleRequest(Request(groupReservationId: groupReservationId), _mockContext.Object);

        AssertError(response, 400, "Missing 'groupReservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_PathParametersNull_Returns400ForGroupReservationIdAndDoesNotCallService()
    {
        ArrangeServiceReturns(SampleResponse());
        var request = Request();
        request.PathParameters = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertError(response, 400, "Missing 'groupReservationId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleRequest_GuestSessionIdMissingOrEmpty_Returns400AndDoesNotCallService(string? guestSessionId)
    {
        ArrangeServiceReturns(SampleResponse());

        var response = await _handler.HandleRequest(Request(guestSessionId: guestSessionId), _mockContext.Object);

        AssertError(response, 400, "Missing 'guestSessionId' in path parameters.");
        VerifyServiceNeverCalled();
    }

    // Unauthenticated caller

    [Fact]
    public async Task HandleRequest_NoSubClaim_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturns(SampleResponse());

        var response = await _handler.HandleRequest(Request(sub: null), _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleRequest_SubEmptyOrWhitespace_Returns401AndDoesNotCallService(string sub)
    {
        ArrangeServiceReturns(SampleResponse());

        var response = await _handler.HandleRequest(Request(sub: sub), _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_RequestContextNull_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturns(SampleResponse());
        var request = Request();
        request.RequestContext = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_AuthorizerNull_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturns(SampleResponse());
        var request = Request();
        request.RequestContext.Authorizer = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    [Fact]
    public async Task HandleRequest_ClaimsNull_Returns401AndDoesNotCallService()
    {
        ArrangeServiceReturns(SampleResponse());
        var request = Request();
        request.RequestContext.Authorizer.Claims = null;

        var response = await _handler.HandleRequest(request, _mockContext.Object);

        AssertUnauthorized(response);
        VerifyServiceNeverCalled();
    }

    // Success

    [Fact]
    public async Task HandleRequest_ValidRequest_CallsServiceWithSubGroupIdAndSessionIdOnce()
    {
        ArrangeServiceReturns(SampleResponse());

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        _mockGuestSessionService.Verify(s => s.DeleteGuestSessionAsync(UserId, GroupId, SessionId), Times.Once);
        _mockGuestSessionService.Verify(
            s => s.DeleteGuestSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ValidRequest_ReturnsCamelCaseBodyWithRemainingParticipants()
    {
        ArrangeServiceReturns(SampleResponse());

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        var body = ParseBody(response);
        Assert.False(body.TryGetProperty("Participants", out _));
        Assert.False(body.TryGetProperty("Total", out _));
        Assert.Equal(220m, body.GetProperty("total").GetDecimal());

        var participants = body.GetProperty("participants");
        Assert.Equal(1, participants.GetArrayLength());
        var participant = participants[0];
        Assert.Equal("session2", participant.GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.String, participant.GetProperty("type").ValueKind);
        Assert.Equal("Guest", participant.GetProperty("type").GetString());
        Assert.Equal("Olena", participant.GetProperty("name").GetString());
        Assert.Equal(220m, participant.GetProperty("total").GetDecimal());
        Assert.False(participant.GetProperty("isSubmitted").GetBoolean());

        var items = participant.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        var item = items[0];
        Assert.Equal("dish2", item.GetProperty("dish").GetProperty("id").GetString());
        Assert.Equal("Tom Yum", item.GetProperty("dish").GetProperty("name").GetString());
        Assert.Equal(220m, item.GetProperty("dish").GetProperty("price").GetDecimal());
        Assert.Equal(1, item.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task HandleRequest_NoGuestsRemain_Returns200WithEmptyParticipantsAndZeroTotal()
    {
        var owner = new Participant("owner-group-1", ParticipantType.Owner, "Owner", [], 0, false);
        ArrangeServiceReturns(new GetAllSessionsResponse([], 0) { Owner = owner });

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(
            """{"participants":[],"total":0,"owner":{"id":"owner-group-1","type":"Owner","name":"Owner","items":[],"total":0,"isSubmitted":false}}""",
            response.Body);
    }

    // Service errors

    public static TheoryData<Exception> NotFoundExceptions => new()
    {
        new GroupReservationNotFoundException(),
        new GuestSessionNotFoundException($"Guest session with id {SessionId} not found."),
        new ReservationNotFoundException("Reservation not found.")
    };

    [Theory]
    [MemberData(nameof(NotFoundExceptions))]
    public async Task HandleRequest_NotFoundException_Returns404WithErrorBody(Exception exception)
    {
        ArrangeServiceThrows(exception);

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 404, exception.Message);
    }

    [Fact]
    public async Task HandleRequest_CallerIsNotOwner_Returns403WithErrorBody()
    {
        var exception = new ActionForbiddenException("You are not the owner of this reservation.");
        ArrangeServiceThrows(exception);

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        AssertError(response, 403, exception.Message);
    }

    public static TheoryData<ValidationException, string> ErrorCodeExceptions => new()
    {
        {
            new GroupReservationSubmittedException(),
            """{"message":"Group reservation was submitted.","errorCode":"GroupReservationSubmitted"}"""
        },
        {
            new GroupReservationExpiredException(),
            """{"message":"Group reservation is expired.","errorCode":"GroupReservationExpired"}"""
        },
        {
            new OwnerSessionNotDeletableException(),
            """{"message":"The owner session cannot be deleted.","errorCode":"OwnerSessionNotDeletable"}"""
        }
    };

    [Theory]
    [MemberData(nameof(ErrorCodeExceptions))]
    public async Task HandleRequest_ValidationExceptionWithErrorCode_Returns400WithErrorCodeBody(
        ValidationException exception, string expectedBody)
    {
        ArrangeServiceThrows(exception);

        var response = await _handler.HandleRequest(Request(), _mockContext.Object);

        Assert.Equal(400, response.StatusCode);
        Assert.Equal(expectedBody, response.Body);
    }
}
