using System;

namespace APIHandler.Services.GroupReservations;

public readonly record struct GroupOrderProgress(int OrderedCount, DateTime? LastOrderedAtUtc);
