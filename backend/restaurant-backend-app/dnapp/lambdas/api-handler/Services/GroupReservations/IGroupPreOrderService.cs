using APIHandler.Models.GroupReservation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.GroupReservations;

public interface IGroupPreOrderService
{
    Task<IReadOnlyList<GroupPreOrderDishResponse>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId);
}
