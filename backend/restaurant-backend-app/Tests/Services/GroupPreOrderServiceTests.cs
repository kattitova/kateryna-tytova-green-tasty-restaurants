using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GroupReservation;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Mappers;
using APIHandler.Services.GroupReservations;
using APIHandler.Services.Locations;
using Moq;

namespace APIHandler.Tests.Services;

public class GroupPreOrderServiceTests
{
    private const string UserId = "owner-user-1";
    private const string GroupId = "group-1";
    private const string ReservationId = "reservation-1";

    private readonly Mock<IGroupReservationRepository> _mockGroupReservationRepository = new();
    private readonly Mock<IReservationRepository> _mockReservationRepository = new();
    private readonly Mock<IGuestSessionRepository> _mockGuestSessionRepository = new();
    private readonly Mock<IOrderRepository> _mockOrderRepository = new();
    private readonly Mock<ILocationService> _mockLocationService = new();
    private readonly GroupPreOrderService _service;

    private Order? _submittedOrder;

    public GroupPreOrderServiceTests()
    {
        _service = new GroupPreOrderService(
            _mockGroupReservationRepository.Object,
            _mockReservationRepository.Object,
            _mockGuestSessionRepository.Object,
            _mockOrderRepository.Object,
            _mockLocationService.Object);

        _mockGroupReservationRepository
            .Setup(r => r.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<Order>()))
            .Callback<string, Order>((_, order) => _submittedOrder = order)
            .ReturnsAsync(true);
    }

    // Arrange helpers

    private void ArrangeReservationOwnedBy(string? customerId)
    {
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync(new Reservation
            {
                Id = ReservationId,
                TableId = "table-1",
                WaiterId = "waiter-1",
                CustomerId = customerId
            });
    }

    private void ArrangeGroup(GroupReservationStatus status = GroupReservationStatus.Open, string reservationId = ReservationId)
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync(new GroupReservation
            {
                GroupReservationId = GroupId,
                ReservationId = reservationId,
                ReservationToken = "token-1",
                Status = status
            });
    }

    private void ArrangeSessions(params GuestSession[] sessions)
    {
        _mockGuestSessionRepository
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupId))
            .ReturnsAsync(sessions.ToList());
    }

    private Order ArrangeOrder(params OrderItem[] items)
    {
        var order = new Order
        {
            OrderId = "order-1",
            CustomerId = UserId,
            ReservationId = ReservationId,
            ReservationAddress = "Address 1",
            ReservationTime = "12:00",
            Dishes = items.ToList(),
            Status = OrderStatus.Created
        };
        _mockOrderRepository
            .Setup(r => r.GetOrderByReservationIdAsync(ReservationId))
            .ReturnsAsync(order);
        return order;
    }

    private void ArrangeValidOwnerAndOpenGroup()
    {
        ArrangeReservationOwnedBy(UserId);
        ArrangeGroup();
    }

    // Task example: A {dish1 x2, dish2 x1}, B {dish1 x1, dish2 x1}, unsubmitted C {dish3 x5}.
    private void ArrangeTaskExampleSessions()
    {
        ArrangeSessions(
            Session("guest-a", isSubmitted: true, items: [("dish1", 2), ("dish2", 1)]),
            Session("guest-b", isSubmitted: true, items: [("dish1", 1), ("dish2", 1)]),
            Session("guest-c", isSubmitted: false, items: ("dish3", 5)));
    }

    private static GuestSession Session(string id, bool isSubmitted, bool isOwnerSession = false, params (string DishId, int Quantity)[] items) => new()
    {
        Id = id,
        GroupReservationId = GroupId,
        GuestName = id,
        IsSubmitted = isSubmitted,
        IsOwnerSession = isOwnerSession,
        Dishes = items.Select(i => new OrderItem { DishId = i.DishId, Quantity = i.Quantity, State = OrderItemState.Pending }).ToList()
    };

    private static OrderItem Item(string dishId, int quantity, OrderItemState state) =>
        new() { DishId = dishId, Quantity = quantity, State = state };

    private Task<IReadOnlyList<GroupPreOrderDishResponse>> Submit(string userId = UserId) =>
        _service.SubmitGroupPreOrderAsync(userId, GroupId, ReservationId);

    private void VerifyNoWrites()
    {
        _mockGroupReservationRepository.Verify(r => r.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<Order>()), Times.Never);
        VerifyNoSeparateSaves();
    }

    private void VerifyNoSeparateSaves()
    {
        _mockOrderRepository.Verify(r => r.SaveOrderAsync(It.IsAny<Order>()), Times.Never);
        _mockGroupReservationRepository.Verify(r => r.SaveGroupReservationAsync(It.IsAny<GroupReservation>()), Times.Never);
        _mockGuestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    private static void AssertItem(OrderItem item, string dishId, int quantity, OrderItemState state)
    {
        Assert.Equal(dishId, item.DishId);
        Assert.Equal(quantity, item.Quantity);
        Assert.Equal(state, item.State);
    }

    // Reservation and owner checks

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ReservationNotFound_ThrowsReservationNotFoundException()
    {
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync((Reservation?)null);
        ArrangeGroup();

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => Submit());

        _mockGroupReservationRepository.Verify(r => r.GetGroupReservationByIdAsync(It.IsAny<string>()), Times.Never);
        VerifyNoWrites();
    }

    [Theory]
    [InlineData("another-user")]
    [InlineData("OWNER-USER-1")]
    public async Task SubmitGroupPreOrderAsync_CallerIsNotOwner_ThrowsActionForbiddenExceptionBeforeGroupLookup(string customerId)
    {
        ArrangeReservationOwnedBy(customerId);
        ArrangeGroup();
        ArrangeTaskExampleSessions();
        ArrangeOrder();

        var exception = await Assert.ThrowsAsync<ActionForbiddenException>(() => Submit());

        Assert.Equal("Only the reservation owner can perform this action.", exception.Message);
        _mockGroupReservationRepository.Verify(r => r.GetGroupReservationByIdAsync(It.IsAny<string>()), Times.Never);
        VerifyNoWrites();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ReservationHasNoCustomer_ThrowsActionForbiddenException()
    {
        ArrangeReservationOwnedBy(null);
        ArrangeGroup();

        await Assert.ThrowsAsync<ActionForbiddenException>(() => Submit());

        _mockGroupReservationRepository.Verify(r => r.GetGroupReservationByIdAsync(It.IsAny<string>()), Times.Never);
        VerifyNoWrites();
    }

    // Group checks (D2, D3)

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupNotFound_ThrowsGroupReservationNotFoundException()
    {
        ArrangeReservationOwnedBy(UserId);
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => Submit());

        VerifyNoWrites();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupBelongsToAnotherReservation_ThrowsGroupReservationNotFoundException()
    {
        ArrangeReservationOwnedBy(UserId);
        ArrangeGroup(reservationId: "reservation-2");
        ArrangeTaskExampleSessions();
        ArrangeOrder();

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => Submit());

        VerifyNoWrites();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupSubmitted_ThrowsGroupReservationSubmittedException()
    {
        ArrangeReservationOwnedBy(UserId);
        ArrangeGroup(GroupReservationStatus.Submitted);
        ArrangeTaskExampleSessions();
        ArrangeOrder();

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => Submit());

        VerifyNoWrites();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupClosed_ThrowsGroupReservationExpiredException()
    {
        ArrangeReservationOwnedBy(UserId);
        ArrangeGroup(GroupReservationStatus.Closed);
        ArrangeTaskExampleSessions();
        ArrangeOrder();

        await Assert.ThrowsAsync<GroupReservationExpiredException>(() => Submit());

        VerifyNoWrites();
    }

    // Session and dish checks (D4)

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(true, OrderItemState.Canceled, false)]
    [InlineData(true, null, true)]
    public async Task SubmitGroupPreOrderAsync_NothingToSubmit_ThrowsValidationExceptionAndWritesNothing(
        bool orderExists, OrderItemState? itemState, bool submittedGuestWithoutDishes)
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeSessions(submittedGuestWithoutDishes
            ? [Session("guest-a", isSubmitted: true), Session("guest-b", isSubmitted: false, items: ("dish1", 2))]
            : []);
        if (orderExists)
        {
            ArrangeOrder(itemState is null ? [] : [Item("dish1", 2, itemState.Value)]);
        }

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Submit());

        Assert.Equal("Nothing to submit: the group has no pre-orders.", exception.Message);
        VerifyNoWrites();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_OwnerOrderAndNoSubmittedGuests_SubmitsOwnerItemsAndReturnsEmptyList()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeSessions(Session("guest-a", isSubmitted: false, items: ("dish3", 5)));
        ArrangeOrder(Item("dish1", 2, OrderItemState.Confirmed), Item("dish2", 1, OrderItemState.Pending));

        IReadOnlyList<GroupPreOrderDishResponse> result = await Submit();

        Assert.Empty(result);
        _mockGroupReservationRepository.Verify(r => r.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<Order>()), Times.Once);
        Assert.NotNull(_submittedOrder);
        Assert.Equal(OrderStatus.SubmittedByCustomer, _submittedOrder.Status);
        Assert.Equal(2, _submittedOrder.Dishes.Count);
        AssertItem(_submittedOrder.Dishes[0], "dish1", 2, OrderItemState.Confirmed);
        AssertItem(_submittedOrder.Dishes[1], "dish2", 1, OrderItemState.Pending);
        _mockLocationService.Verify(l => l.GetLocationByTableIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoOrderAndSubmittedGuests_CreatesOrderWithConsolidatedDishes()
    {
        var start = new DateTime(2030, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(2);
        ArrangeGroup();
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ReservationId))
            .ReturnsAsync(new Reservation
            {
                Id = ReservationId,
                TableId = "table-1",
                WaiterId = "waiter-1",
                CustomerId = UserId,
                StartTime = start,
                EndTime = end
            });
        _mockLocationService
            .Setup(l => l.GetLocationByTableIdAsync("table-1"))
            .ReturnsAsync(new Location { Id = "loc-1", Address = "48 Rustaveli Avenue", Description = "d", AverageOccupancy = "Low" });
        ArrangeTaskExampleSessions();
        _mockOrderRepository
            .Setup(r => r.GetOrderByReservationIdAsync(ReservationId))
            .ReturnsAsync((Order?)null);

        IReadOnlyList<GroupPreOrderDishResponse> result = await Submit();

        Assert.Equal([new("dish1", 3), new("dish2", 2)], result);
        _mockGroupReservationRepository.Verify(r => r.SubmitGroupPreOrderAsync(GroupId, It.IsAny<Order>()), Times.Once);
        Assert.NotNull(_submittedOrder);
        Assert.False(string.IsNullOrWhiteSpace(_submittedOrder.OrderId));
        Assert.Equal(UserId, _submittedOrder.CustomerId);
        Assert.Equal(ReservationId, _submittedOrder.ReservationId);
        Assert.Equal("48 Rustaveli Avenue", _submittedOrder.ReservationAddress);
        Assert.Equal(OrderMapper.FormatReservationTime(start, end), _submittedOrder.ReservationTime);
        Assert.Equal(OrderStatus.SubmittedByCustomer, _submittedOrder.Status);
        Assert.Equal(2, _submittedOrder.Dishes.Count);
        AssertItem(_submittedOrder.Dishes[0], "dish1", 3, OrderItemState.Pending);
        AssertItem(_submittedOrder.Dishes[1], "dish2", 2, OrderItemState.Pending);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoOrderAndLocationMissing_ThrowsValidationExceptionBeforeTransaction()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        _mockOrderRepository
            .Setup(r => r.GetOrderByReservationIdAsync(ReservationId))
            .ReturnsAsync((Order?)null);
        _mockLocationService
            .Setup(l => l.GetLocationByTableIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Location)null!);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => Submit());

        Assert.Equal("Location not found for the reservation's table.", exception.Message);
        VerifyNoWrites();
    }

    // Consolidation and response

    [Fact]
    public async Task SubmitGroupPreOrderAsync_TaskExample_ReturnsConsolidatedSubmittedDishesInFirstAppearanceOrder()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        ArrangeOrder();

        IReadOnlyList<GroupPreOrderDishResponse> result = await Submit();

        Assert.Equal([new("dish1", 3), new("dish2", 2)], result);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_FirstAppearanceNotSorted_KeepsFirstAppearanceOrder()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeSessions(
            Session("guest-a", isSubmitted: true, items: [("dish2", 1), ("dish1", 1)]),
            Session("guest-b", isSubmitted: true, items: [("dish3", 4), ("dish1", 2)]));
        ArrangeOrder();

        IReadOnlyList<GroupPreOrderDishResponse> result = await Submit();

        Assert.Equal([new("dish2", 1), new("dish1", 3), new("dish3", 4)], result);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ExistingOrderItems_ResponseHasOnlyConsolidatedGuestDishes()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        ArrangeOrder(
            Item("dish1", 1, OrderItemState.Confirmed),
            Item("dish9", 2, OrderItemState.Confirmed));

        IReadOnlyList<GroupPreOrderDishResponse> result = await Submit();

        Assert.Equal([new("dish1", 3), new("dish2", 2)], result);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_SubmittedOwnerSessionWithDishes_IsIncluded()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeSessions(
            Session("owner-group-1", isSubmitted: true, isOwnerSession: true, items: ("dish1", 2)),
            Session("guest-a", isSubmitted: true, items: ("dish1", 1)));
        ArrangeOrder();

        IReadOnlyList<GroupPreOrderDishResponse> result = await Submit();

        Assert.Equal([new("dish1", 3)], result);
    }

    // Writes

    [Fact]
    public async Task SubmitGroupPreOrderAsync_HappyPath_SubmitsMergedOrderOnceInOneTransaction()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        Order order = ArrangeOrder();

        await Submit();

        _mockGroupReservationRepository.Verify(r => r.SubmitGroupPreOrderAsync(GroupId, order), Times.Once);
        _mockGroupReservationRepository.Verify(r => r.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<Order>()), Times.Once);
        VerifyNoSeparateSaves();

        Assert.NotNull(_submittedOrder);
        Assert.Equal(OrderStatus.SubmittedByCustomer, _submittedOrder.Status);
        Assert.Equal(2, _submittedOrder.Dishes.Count);
        AssertItem(_submittedOrder.Dishes[0], "dish1", 3, OrderItemState.Pending);
        AssertItem(_submittedOrder.Dishes[1], "dish2", 2, OrderItemState.Pending);
        Assert.DoesNotContain(_submittedOrder.Dishes, d => d.DishId == "dish3");
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_RepositoryConditionFails_ThrowsGroupReservationSubmittedException()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        ArrangeOrder();
        _mockGroupReservationRepository
            .Setup(r => r.SubmitGroupPreOrderAsync(It.IsAny<string>(), It.IsAny<Order>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => Submit());

        _mockGroupReservationRepository.Verify(r => r.SubmitGroupPreOrderAsync(GroupId, It.IsAny<Order>()), Times.Once);
        VerifyNoSeparateSaves();
    }

    // Merge into the existing order (D5, D5b)

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ExistingItems_AddsToSameDishKeepsOthersAndAppendsNew()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        ArrangeOrder(
            Item("dish1", 1, OrderItemState.Confirmed),
            Item("dish9", 2, OrderItemState.Confirmed));

        await Submit();

        Assert.NotNull(_submittedOrder);
        Assert.Equal(3, _submittedOrder.Dishes.Count);
        AssertItem(_submittedOrder.Dishes[0], "dish1", 4, OrderItemState.Pending);
        AssertItem(_submittedOrder.Dishes[1], "dish9", 2, OrderItemState.Confirmed);
        AssertItem(_submittedOrder.Dishes[2], "dish2", 2, OrderItemState.Pending);
        Assert.Equal(OrderStatus.SubmittedByCustomer, _submittedOrder.Status);
    }

    [Theory]
    [InlineData(OrderItemState.Pending)]
    [InlineData(OrderItemState.Confirmed)]
    public async Task SubmitGroupPreOrderAsync_ActiveItemWithSameDish_AddsQuantityAndSetsPending(OrderItemState state)
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeSessions(Session("guest-a", isSubmitted: true, items: ("dish1", 3)));
        ArrangeOrder(Item("dish1", 1, state));

        await Submit();

        Assert.NotNull(_submittedOrder);
        var item = Assert.Single(_submittedOrder.Dishes);
        AssertItem(item, "dish1", 4, OrderItemState.Pending);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_CanceledItemWithSameDish_TakesGuestQuantityAndSetsPending()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeSessions(Session("guest-a", isSubmitted: true, items: ("dish1", 3)));
        ArrangeOrder(Item("dish1", 5, OrderItemState.Canceled));

        await Submit();

        Assert.NotNull(_submittedOrder);
        var item = Assert.Single(_submittedOrder.Dishes);
        AssertItem(item, "dish1", 3, OrderItemState.Pending);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_OrderWithoutItems_GetsExactlyConsolidatedPendingItems()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        ArrangeOrder();

        await Submit();

        Assert.NotNull(_submittedOrder);
        Assert.Equal(2, _submittedOrder.Dishes.Count);
        AssertItem(_submittedOrder.Dishes[0], "dish1", 3, OrderItemState.Pending);
        AssertItem(_submittedOrder.Dishes[1], "dish2", 2, OrderItemState.Pending);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_OrderDishesNull_TreatsAsEmpty()
    {
        ArrangeValidOwnerAndOpenGroup();
        ArrangeTaskExampleSessions();
        Order order = ArrangeOrder();
        order.Dishes = null!;

        IReadOnlyList<GroupPreOrderDishResponse> result = await Submit();

        Assert.Equal([new("dish1", 3), new("dish2", 2)], result);
        Assert.NotNull(_submittedOrder);
        Assert.NotNull(_submittedOrder.Dishes);
        Assert.Equal(2, _submittedOrder.Dishes.Count);
        AssertItem(_submittedOrder.Dishes[0], "dish1", 3, OrderItemState.Pending);
        AssertItem(_submittedOrder.Dishes[1], "dish2", 2, OrderItemState.Pending);
    }
}
