using APIHandler.Entities;
using System.Collections.Generic;
using System.Linq;

namespace APIHandler.Helpers;

public static class GuestSessionExtensions
{
    public static IEnumerable<GuestSession> GuestsOnly(this IEnumerable<GuestSession> sessions)
        => sessions.Where(s => !s.IsOwnerSession);
}
