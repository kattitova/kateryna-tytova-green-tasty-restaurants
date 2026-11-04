using Amazon.Lambda.Core;
using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Services.Orders;

public interface IOrderService
{
    Task<Order?> GetOrderByReservationIdAsync(string reservationId);

    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId);

    Task AddOrderAsync(Order order);

    Task AddOrderForWaiterAsync(Order order);

    Task ConfirmPreOrderDishAsync(string callerId, string reservationId, string dishId, ILambdaLogger logger);

    Task CancelPreOrderDishAsync(string callerId, string reservationId, string dishId, ILambdaLogger logger);

    Task EditPreOrderAsync(string customerId, string reservationId, List<OrderItem> newDishes, ILambdaContext context);

    Task EditPreOrderForWaiterAsync(string waiterId, string reservationId, List<OrderItem> newDishes, ILambdaContext context);

    Task CancelPreOrderAsync(string customerId, string reservationId, ILambdaContext context);

    /// <summary>
    /// Cancels the active pre-order of a reservation, if there is one. Call it after the reservation is cancelled.
    /// The caller is responsible for the authorization check.
    /// </summary>
    Task CancelPreOrderForReservationAsync(string reservationId);

    Task SubmitPreOrderAsync(string customerId, string reservationId, ILambdaContext context);

    Task UnSubmitPreOrderAsync(string customerId, string reservationId, ILambdaContext context);
}