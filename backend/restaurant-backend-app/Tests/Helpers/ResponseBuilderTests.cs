using APIHandler.Exceptions;
using APIHandler.Helpers;
using System.Text.Json;

namespace APIHandler.Tests.Helpers;

public class ResponseBuilderTests
{
    private static JsonElement ParseBody(string body) => JsonDocument.Parse(body).RootElement;

    [Fact]
    public void CreateErrorCodeResponse_OwnerSessionNotDeletable_Returns400WithOwnerSessionNotDeletableCode()
    {
        var response = ResponseBuilder.CreateErrorCodeResponse(new OwnerSessionNotDeletableException());

        Assert.Equal(400, response.StatusCode);
        Assert.Equal(
            """{"message":"The owner session cannot be deleted.","errorCode":"OwnerSessionNotDeletable"}""",
            response.Body);

        var body = ParseBody(response.Body);
        Assert.Equal("The owner session cannot be deleted.", body.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.String, body.GetProperty("errorCode").ValueKind);
        Assert.Equal("OwnerSessionNotDeletable", body.GetProperty("errorCode").GetString());
        Assert.False(body.TryGetProperty("Message", out _));
        Assert.False(body.TryGetProperty("ErrorCode", out _));
    }

    public static TheoryData<ValidationException, string, string> ExistingMappings => new()
    {
        { new GroupReservationSubmittedException(), "GroupReservationSubmitted", "Group reservation was submitted." },
        { new GroupReservationExpiredException(), "GroupReservationExpired", "Group reservation is expired." },
        { new ValidationException("x"), "Unknown", "x" }
    };

    [Theory]
    [MemberData(nameof(ExistingMappings))]
    public void CreateErrorCodeResponse_ExistingExceptions_KeepTheirErrorCodes(
        ValidationException exception, string expectedErrorCode, string expectedMessage)
    {
        var response = ResponseBuilder.CreateErrorCodeResponse(exception);

        Assert.Equal(400, response.StatusCode);
        var body = ParseBody(response.Body);
        Assert.Equal(expectedErrorCode, body.GetProperty("errorCode").GetString());
        Assert.Equal(expectedMessage, body.GetProperty("message").GetString());
    }
}
