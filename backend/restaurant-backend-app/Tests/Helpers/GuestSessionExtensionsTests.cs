using APIHandler.Entities;
using APIHandler.Helpers;

namespace APIHandler.Tests.Helpers;

public class GuestSessionExtensionsTests
{
    private static GuestSession Session(string id, bool isOwnerSession = false) => new()
    {
        Id = id,
        GroupReservationId = "group-1",
        GuestName = id,
        Dishes = [],
        IsOwnerSession = isOwnerSession
    };

    [Fact]
    public void GuestsOnly_MixedSessions_ReturnsOnlyNonOwnerSessionsInInputOrder()
    {
        List<GuestSession> sessions =
        [
            Session("zeta"),
            Session("owner-group-1", isOwnerSession: true),
            Session("alpha"),
            Session("owner-other", isOwnerSession: true),
            Session("mid")
        ];

        List<string> result = sessions.GuestsOnly().Select(s => s.Id).ToList();

        Assert.Equal(["zeta", "alpha", "mid"], result);
    }

    [Fact]
    public void GuestsOnly_EmptyList_ReturnsEmpty()
    {
        List<GuestSession> sessions = [];

        Assert.Empty(sessions.GuestsOnly());
    }
}
