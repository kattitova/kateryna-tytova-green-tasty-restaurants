using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.GuestSessions;
using AutoFixture.Xunit2;
using Moq;
using NPOI.POIFS.Storage;
using System.Security.Cryptography;
using System.Text;

namespace APIHandler.Tests.Services;

public class GuestSessionServiceTests
{
    private readonly Mock<IGuestSessionRepository> _mockGuestSessionRepository = new();
    private readonly Mock<IGroupReservationRepository> _mockGroupReservationRepository = new();
    private readonly Mock<IDishRepository> _mockDishRepository = new();
    private readonly Mock<IReservationRepository> _mockReservationRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<ITableRepository> _mockTableRepository = new();
    private readonly Mock<IOrderRepository> _mockOrderRepository = new();
    private readonly GuestSessionService _guestSessionService;

    public GuestSessionServiceTests()
    {
        _guestSessionService = new GuestSessionService(
            _mockGroupReservationRepository.Object,
            _mockGuestSessionRepository.Object,
            _mockDishRepository.Object,
            _mockReservationRepository.Object,
            _mockUserRepository.Object,
            _mockTableRepository.Object,
            _mockOrderRepository.Object);
    }
    [Theory]
    [InlineAutoData(0)]
    [InlineAutoData(3)]
    public async Task CreateGuestSessionAsync_CorrectData_ReturnsSessionToken(
        int guestCount,
        CreateGuestSessionRequest request,
        GroupReservation groupReservation,
        Reservation parentReservation)
    {
        groupReservation.GroupReservationId = request.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;
        groupReservation.ReservationId = parentReservation.Id;

        parentReservation.GuestCount = 5;

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(groupReservation);

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(parentReservation.Id))
            .ReturnsAsync(parentReservation);

        _mockGuestSessionRepository
            .Setup(r => r.GetAllByGroupReservationIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(GuestSessions(guestCount));

        GuestSession? session = null;
        _mockGuestSessionRepository
            .Setup(r => r.SaveAsync(It.IsAny<GuestSession>()))
            .Callback<GuestSession>(s => session = s);

        var response = await _guestSessionService.CreateGuestSessionAsync(request);

        Assert.NotEmpty(response.SessionId);
        Assert.NotEmpty(response.SessionToken);
        Assert.NotNull(session);
        Assert.Equal(groupReservation.GroupReservationId, session.GroupReservationId);
        Assert.Equal(request.GuestName, session.GuestName);
        Assert.NotNull(session.SessionTokenHash);
        Assert.NotEmpty(session.SessionTokenHash);
    }

    [Theory]
    [AutoData]
    public async Task CreateGuestSessionAsync_InvalidGroupReservationId_ThrowsGroupReservationNotFoundException(CreateGuestSessionRequest request)
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(request.GroupReservationId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _guestSessionService.CreateGuestSessionAsync(request));
    }

    [Theory]
    [AutoData]
    public async Task CreateGuestSessionAsync_GroupReservationExpired_ThrowsGroupReservationExpiredException(CreateGuestSessionRequest request, GroupReservation groupReservation)
    {
        groupReservation.GroupReservationId = request.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(request.GroupReservationId))
            .ReturnsAsync(groupReservation);

        await Assert.ThrowsAsync<GroupReservationExpiredException>(() => _guestSessionService.CreateGuestSessionAsync(request));
    }

    [Theory]
    [AutoData]
    public async Task AddDishAsync_ShouldAddNewDish_WhenDishDoesNotExistInSession(GuestSession guestSession, GroupReservation groupReservation, Reservation parentReservation, Table table, Dish dish)
    {
        guestSession.Dishes = [];
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        groupReservation.ReservationId = parentReservation.Id;
        parentReservation.TableId = table.Id;
        dish.State = DishState.Available;
        int quantityToAdd = 2;

        _mockGuestSessionRepository.Setup(r => r.GetByGuestSessionId(guestSession.Id)).ReturnsAsync(guestSession);
        _mockGroupReservationRepository.Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId)).ReturnsAsync(groupReservation);
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(parentReservation.Id)).ReturnsAsync(parentReservation);
        _mockTableRepository.Setup(r => r.GetTableAsync(table.Id)).ReturnsAsync(table);
        _mockDishRepository.Setup(r => r.GetDishesByLocationIdAsync(table.LocationId)).ReturnsAsync([dish]);

        await _guestSessionService.AddDishAsync(guestSession.Id, dish.Id, quantityToAdd);

        Assert.Single(guestSession.Dishes);
        Assert.Equal(dish.Id, guestSession.Dishes[0].DishId);
        Assert.Equal(quantityToAdd, guestSession.Dishes[0].Quantity);
        Assert.Equal(OrderItemState.Pending, guestSession.Dishes[0].State);

        _mockGuestSessionRepository.Verify(r => r.SaveAsync(guestSession), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task AddDishAsync_ShouldIncreaseQuantity_WhenDishAlreadyExists(GuestSession guestSession, GroupReservation groupReservation, Reservation parentReservation, Table table, Dish dish)
    {
        int initialQuantity = 3;
        int quantityToAdd = 2;
        guestSession.Dishes = [new OrderItem { DishId = dish.Id, Quantity = initialQuantity, State = OrderItemState.Pending }];
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        groupReservation.ReservationId = parentReservation.Id;
        parentReservation.TableId = table.Id;
        dish.State = DishState.Available;

        _mockGuestSessionRepository.Setup(r => r.GetByGuestSessionId(guestSession.Id)).ReturnsAsync(guestSession);
        _mockGroupReservationRepository.Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId)).ReturnsAsync(groupReservation);
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(parentReservation.Id)).ReturnsAsync(parentReservation);
        _mockTableRepository.Setup(r => r.GetTableAsync(table.Id)).ReturnsAsync(table);
        _mockDishRepository.Setup(r => r.GetDishesByLocationIdAsync(table.LocationId)).ReturnsAsync([dish]);

        await _guestSessionService.AddDishAsync(guestSession.Id, dish.Id, quantityToAdd);

        Assert.Single(guestSession.Dishes);
        Assert.Equal(initialQuantity + quantityToAdd, guestSession.Dishes[0].Quantity);

        _mockGuestSessionRepository.Verify(r => r.SaveAsync(guestSession), Times.Once);
    }

    [Theory]
    [InlineAutoData(0)]
    [InlineAutoData(-1)]
    public async Task AddDishAsync_ShouldThrowValidationException_WhenQuantityIsZeroOrLess(int quantity, string guestSessionId, Dish dish)
    {
        dish.State = DishState.Available;

        _mockDishRepository
            .Setup(r => r.GetDishByIdAsync(dish.Id))
            .ReturnsAsync(dish);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _guestSessionService.AddDishAsync(guestSessionId, dish.Id, quantity));
    }

    [Theory]
    [AutoData]
    public async Task AddDishAsync_ShouldThrowGuestSessionNotFoundException_WhenSessionNotFound(string guestSessionId, Dish dish)
    {
        dish.State = DishState.Available;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSessionId))
            .ReturnsAsync((GuestSession?)null);

        _mockDishRepository
            .Setup(r => r.GetDishByIdAsync(dish.Id))
            .ReturnsAsync(dish);

        var exception = await Assert.ThrowsAsync<GuestSessionNotFoundException>(() =>
            _guestSessionService.AddDishAsync(guestSessionId, dish.Id, 2));
    }

    [Theory]
    [AutoData]
    public async Task AddDishAsync_ShouldThrowDishNotFound_WhenDishNotFound(GuestSession guestSession, GroupReservation groupReservation, Reservation parentReservation, Table table, string dishId)
    {
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;
        groupReservation.ReservationId = parentReservation.Id;
        parentReservation.TableId = table.Id;

        _mockGuestSessionRepository.Setup(r => r.GetByGuestSessionId(guestSession.Id)).ReturnsAsync(guestSession);
        _mockGroupReservationRepository.Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId)).ReturnsAsync(groupReservation);
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(parentReservation.Id)).ReturnsAsync(parentReservation);
        _mockTableRepository.Setup(r => r.GetTableAsync(table.Id)).ReturnsAsync(table);

        _mockDishRepository.Setup(r => r.GetDishesByLocationIdAsync(table.LocationId)).ReturnsAsync([]);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _guestSessionService.AddDishAsync(guestSession.Id, dishId, quantity: 2));
    }

    [Theory]
    [AutoData]
    public async Task AddDishAsync_ShouldThrowDishNotAvailable_WhenDishIsNotAvailable(GuestSession guestSession, GroupReservation groupReservation, Reservation parentReservation, Table table, Dish dish)
    {
        dish.State = DishState.OutOfStock;
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;
        groupReservation.ReservationId = parentReservation.Id;
        parentReservation.TableId = table.Id;

        _mockGuestSessionRepository.Setup(r => r.GetByGuestSessionId(guestSession.Id)).ReturnsAsync(guestSession);
        _mockGroupReservationRepository.Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId)).ReturnsAsync(groupReservation);
        _mockReservationRepository.Setup(r => r.GetReservationByIdAsync(parentReservation.Id)).ReturnsAsync(parentReservation);
        _mockTableRepository.Setup(r => r.GetTableAsync(table.Id)).ReturnsAsync(table);
        _mockDishRepository.Setup(r => r.GetDishesByLocationIdAsync(table.LocationId)).ReturnsAsync([dish]);

        var exception = await Assert.ThrowsAsync<DishIsNotAvailableException>(() =>
            _guestSessionService.AddDishAsync(guestSession.Id, dish.Id, quantity: 2));
    }

    [Theory]
    [InlineAutoData(GroupReservationStatus.Submitted, typeof(GroupReservationSubmittedException))]
    [InlineAutoData(GroupReservationStatus.Closed, typeof(GroupReservationExpiredException))]
    public async Task AddDishAsync_ShouldThrowGroupReservationExpired_WhenGroupStatusIsNotOpen(GroupReservationStatus status, Type exceptionType, GuestSession guestSession, GroupReservation groupReservation, Dish dish)
    {
        dish.State = DishState.Available;
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = status;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockDishRepository
            .Setup(r => r.GetDishByIdAsync(dish.Id))
            .ReturnsAsync(dish);

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(groupReservation);

        var exception = await Assert.ThrowsAsync(exceptionType, () =>
            _guestSessionService.AddDishAsync(guestSession.Id, dish.Id, quantity: 2));
    }

    [Theory]
    [AutoData]
    public async Task RemoveDishAsync_ShouldDecreaseQuantity_WhenQuantityIsGreaterThanAmountToRemove(GuestSession guestSession, GroupReservation groupReservation, string dishId)
    {
        int initialQuantity = 5;
        int quantityToRemove = 2;
        guestSession.Dishes =
        [
            new OrderItem { DishId = dishId, Quantity = initialQuantity, State = OrderItemState.Pending }
        ];
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);


        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(groupReservation);

        await _guestSessionService.RemoveDishAsync(guestSession.Id, dishId, quantityToRemove);

        Assert.Single(guestSession.Dishes);
        Assert.Equal(initialQuantity - quantityToRemove, guestSession.Dishes[0].Quantity);

        _mockGuestSessionRepository.Verify(r => r.SaveAsync(guestSession), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task RemoveDishAsync_ShouldRemoveDishFromList_WhenQuantityToRemoveIsEqualOrGreater(GuestSession guestSession, GroupReservation groupReservation, string dishId)
    {
        int initialQuantity = 2;
        int quantityToRemove = 3;
        guestSession.Dishes =
        [
            new OrderItem { DishId = dishId, Quantity = initialQuantity, State = OrderItemState.Pending }
        ];
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(groupReservation);

        await _guestSessionService.RemoveDishAsync(guestSession.Id, dishId, quantityToRemove);

        Assert.Empty(guestSession.Dishes);
        _mockGuestSessionRepository.Verify(r => r.SaveAsync(guestSession), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task RemoveDishAsync_ShouldThrowDishNotFoundException_WhenDishDoesNotExistInSession(GuestSession guestSession, GroupReservation groupReservation, string dishId)
    {
        guestSession.Dishes = [];
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockGroupReservationRepository
           .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
           .ReturnsAsync(groupReservation);

        var exception = await Assert.ThrowsAsync<DishNotFoundException>(() =>
            _guestSessionService.RemoveDishAsync(guestSession.Id, dishId, 2));

        Assert.Contains("not found in guest session", exception.Message);
    }

    [Theory]
    [AutoData]
    public async Task RemoveDishAsync_ShouldThrowValidationException_WhenQuantityIsZeroOrLess(string guestSessionId, string dishId)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _guestSessionService.RemoveDishAsync(guestSessionId, dishId, 0));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _guestSessionService.RemoveDishAsync(guestSessionId, dishId, -5));
    }

    [Theory]
    [AutoData]
    public async Task RemoveDishAsync_ShouldThrowGuestSessionNotFoundException_WhenSessionNotFound(string guestSessionId, string dishId)
    {
        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSessionId))
            .ReturnsAsync((GuestSession?)null);

        var exception = await Assert.ThrowsAsync<GuestSessionNotFoundException>(() =>
            _guestSessionService.RemoveDishAsync(guestSessionId, dishId, 2));
    }
    [Theory]
    [InlineAutoData(1, 0)]
    [InlineAutoData(1, 1)]
    [InlineAutoData(2, 1)]
    [InlineAutoData(2, 2)]
    public async Task CreateGuestSessionAsync_WhenCapacityReached_ThrowsGuestLimitExceededException(
        int reservationCapacity,
        int guestCount,
        CreateGuestSessionRequest request,
        GroupReservation groupReservation,
        Reservation parentReservation)
    {
        groupReservation.Status = GroupReservationStatus.Open;
        groupReservation.GroupReservationId = request.GroupReservationId;
        groupReservation.ReservationId = parentReservation.Id;

        parentReservation.GuestCount = reservationCapacity;

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(groupReservation);

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(parentReservation.Id))
            .ReturnsAsync(parentReservation);

        _mockGuestSessionRepository
            .Setup(r => r.GetAllByGroupReservationIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(GuestSessions(guestCount));

        var exception = await Assert.ThrowsAsync<GuestLimitExceededException>(() => _guestSessionService.CreateGuestSessionAsync(request));

        _mockGuestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task CreateGuestSessionAsync_OwnerSessionAndOneGuestWithCapacityThree_CreatesSession()
    {
        ArrangeOpenGroupWithReservation(guestCount: 3);
        ArrangeSessions(
            Session("owner-group-1", "Owner", isOwnerSession: true),
            Session("guest-a", "Andrii"));

        GuestSession? saved = null;
        _mockGuestSessionRepository
            .Setup(r => r.SaveAsync(It.IsAny<GuestSession>()))
            .Callback<GuestSession>(s => saved = s);

        CreatGuestSessionResponse response = await _guestSessionService.CreateGuestSessionAsync(
            new CreateGuestSessionRequest(GroupId, "Olena"));

        Assert.NotEmpty(response.SessionId);
        Assert.NotNull(saved);
        Assert.Equal("Olena", saved.GuestName);
        Assert.Equal(GroupId, saved.GroupReservationId);
        _mockGuestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Once);
    }

    [Fact]
    public async Task CreateGuestSessionAsync_TwoGuestsWithCapacityThree_ThrowsGuestLimitExceededException()
    {
        ArrangeOpenGroupWithReservation(guestCount: 3);
        ArrangeSessions(
            Session("guest-a", "Andrii"),
            Session("guest-b", "Olena"));

        await Assert.ThrowsAsync<GuestLimitExceededException>(() => _guestSessionService.CreateGuestSessionAsync(
            new CreateGuestSessionRequest(GroupId, "Ivan")));

        _mockGuestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ParticipantCountCases))]
    public async Task GetGroupReservationStatusAsync_SessionsWithOrWithoutOwner_CountsGuestsPlusOwner(
        string[] sessionKinds,
        int expectedParticipants)
    {
        ArrangeOpenGroupWithReservation(guestCount: 5);
        ArrangeSessions(sessionKinds
            .Select((kind, i) => Session($"{kind}-{i}", kind, isOwnerSession: kind == "owner"))
            .ToArray());

        GroupReservationStatusResponse result = await _guestSessionService.GetGroupReservationStatusAsync(GroupId);

        Assert.Equal(expectedParticipants, result.CurrentParticipants);
        Assert.Equal(5, result.MaxCapacity);
        Assert.Equal(GroupReservationStatus.Open, result.Status);
    }

    [Fact]
    public async Task GetGroupReservationStatusAsync_OwnerOrderedAndTwoOfThreeGuestsSubmitted_CountsThreeAndBonusInProgress()
    {
        ArrangeOpenGroupWithReservation(guestCount: 4, startTime: DateTime.UtcNow.AddDays(3));
        ArrangeSessions(
            SubmittedSession("guest-a", DateTime.UtcNow.AddHours(-3)),
            SubmittedSession("guest-b", DateTime.UtcNow.AddHours(-2)),
            Session("guest-c", "Waiting guest", isSubmitted: false, items: ("dish1", 1)));
        ArrangeOrder(DateTime.UtcNow.AddHours(-4), new OrderItem { DishId = "dish9", Quantity = 1 });

        GroupReservationStatusResponse result = await _guestSessionService.GetGroupReservationStatusAsync(GroupId);

        Assert.Equal(3, result.OrderedCount);
        Assert.Equal(GroupBonusState.InProgress, result.Bonus.State);
        Assert.Equal(GroupBonusTier.TenPercentDiscount, result.Bonus.Tier);
    }

    [Fact]
    public async Task GetGroupReservationStatusAsync_AllSeatsOrderedTwoDaysBeforeStart_ReturnsAchievedTenPercent()
    {
        DateTime orderedAt = DateTime.UtcNow.AddDays(1);
        ArrangeOpenGroupWithReservation(guestCount: 4, startTime: DateTime.UtcNow.AddDays(3));
        ArrangeSessions(
            SubmittedSession("guest-a", orderedAt),
            SubmittedSession("guest-b", orderedAt),
            SubmittedSession("guest-c", orderedAt));
        ArrangeOrder(orderedAt, new OrderItem { DishId = "dish9", Quantity = 1 });

        GroupReservationStatusResponse result = await _guestSessionService.GetGroupReservationStatusAsync(GroupId);

        Assert.Equal(4, result.OrderedCount);
        Assert.Equal(GroupBonusState.Achieved, result.Bonus.State);
        Assert.Equal(GroupBonusTier.TenPercentDiscount, result.Bonus.Tier);
        Assert.Equal(10, result.Bonus.DiscountPercent);
    }

    [Fact]
    public async Task GetGroupReservationStatusAsync_SubmittedGroupWithOnlyGuestDishesInOrder_DoesNotCountOwner()
    {
        ArrangeOpenGroupWithReservation(
            guestCount: 4, startTime: DateTime.UtcNow.AddDays(3), groupStatus: GroupReservationStatus.Submitted);
        ArrangeSessions(Session("guest-a", "Anna", isSubmitted: true, items: ("dish1", 2)));
        ArrangeOrder(DateTime.UtcNow.AddHours(-1), new OrderItem { DishId = "dish1", Quantity = 2 });

        GroupReservationStatusResponse result = await _guestSessionService.GetGroupReservationStatusAsync(GroupId);

        Assert.Equal(1, result.OrderedCount);
    }

    private static GuestSession SubmittedSession(string id, DateTime submittedAt)
    {
        GuestSession session = Session(id, id, isSubmitted: true, items: ("dish1", 1));
        session.SubmittedAt = submittedAt;
        return session;
    }

    private void ArrangeOrder(DateTime createdAt, params OrderItem[] items)
    {
        _mockOrderRepository
            .Setup(r => r.GetOrderByReservationIdAsync(ParentReservationId))
            .ReturnsAsync(new Order
            {
                OrderId = "order-1",
                CustomerId = GroupOwnerUserId,
                ReservationId = ParentReservationId,
                ReservationAddress = "Address",
                ReservationTime = "Time",
                CreatedAt = createdAt,
                Dishes = items.ToList()
            });
    }

    public static TheoryData<string[], int> ParticipantCountCases() => new()
    {
        { ["owner", "guest", "guest"], 3 },
        { ["guest"], 2 },
        { ["owner"], 1 },
    };

    private void ArrangeOpenGroupWithReservation(
        int guestCount,
        DateTime? startTime = null,
        GroupReservationStatus groupStatus = GroupReservationStatus.Open)
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync(new GroupReservation
            {
                GroupReservationId = GroupId,
                ReservationId = ParentReservationId,
                ReservationToken = "token-1",
                Status = groupStatus
            });

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ParentReservationId))
            .ReturnsAsync(new Reservation
            {
                Id = ParentReservationId,
                TableId = "table-1",
                WaiterId = "waiter-1",
                CustomerId = GroupOwnerUserId,
                Status = ReservationStatus.Reserved,
                GuestCount = guestCount,
                StartTime = startTime ?? default
            });
    }

    private static List<GuestSession> GuestSessions(int count) =>
        Enumerable.Range(1, count)
            .Select(i => Session($"guest-{i}", $"Guest {i}"))
            .ToList();

    [Theory]
    [AutoData]
    public async Task CreateGuestSessionAsync_MissingParentReservation_ThrowsGroupReservationExpiredException(
        CreateGuestSessionRequest request,
        GroupReservation groupReservation)
    {
        groupReservation.GroupReservationId = request.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(groupReservation);

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(groupReservation.ReservationId))
            .ReturnsAsync((Reservation?)null);

        var exception = await Assert.ThrowsAsync<GroupReservationExpiredException>(() => _guestSessionService.CreateGuestSessionAsync(request));
    }

    [Theory]
    [AutoData]
    public async Task IsAuthorizedAsync_ValidTokenAndId_ReturnsTrue(GuestSession guestSession, byte[] tokenBytes)
    {
        string tokenString = Convert.ToBase64String(tokenBytes);
        var hashString = Convert.ToBase64String(SHA256.HashData(tokenBytes));
        guestSession.SessionTokenHash = hashString;

        _mockGuestSessionRepository
            .Setup(r => r.GetByTokenHash(hashString))
            .ReturnsAsync(guestSession);

        bool result = await _guestSessionService.IsAuthorizedAsync(tokenString, guestSession.Id);

        Assert.True(result);
    }

    [Theory]
    [AutoData]
    public async Task IsAuthorizedAsync_InvalidToken_ReturnsFalse(byte[] tokenBytes, string guestSessionId)
    {
        string tokenString = Convert.ToBase64String(tokenBytes);
        var hashString = Convert.ToBase64String(SHA256.HashData(tokenBytes));

        _mockGuestSessionRepository
            .Setup(r => r.GetByTokenHash(hashString))
            .ReturnsAsync((GuestSession?)null);

        bool result = await _guestSessionService.IsAuthorizedAsync(tokenString, guestSessionId);

        Assert.False(result);
    }

    [Theory]
    [AutoData]
    public async Task IsAuthorizedAsync_InvalidGuestSessionId_ReturnsFalse(GuestSession guestSession, byte[] tokenBytes, string guestSessionId)
    {
        string tokenString = Convert.ToBase64String(tokenBytes);
        var hashString = Convert.ToBase64String(SHA256.HashData(tokenBytes));
        guestSession.SessionTokenHash = hashString;

        _mockGuestSessionRepository
            .Setup(r => r.GetByTokenHash(hashString))
            .ReturnsAsync(guestSession);

        bool result = await _guestSessionService.IsAuthorizedAsync(tokenString, guestSessionId);

        Assert.False(result);
    }
    [Theory]
    [AutoData]
    public async Task SubmitCartAsync_Valid_SubmitsSuccessfuilly(GuestSession guestSession, GroupReservation groupReservation, Dish[] dishes)
    {
        foreach (var dish in dishes)
            dish.State = DishState.Available;

        guestSession.IsSubmitted = false;
        guestSession.SubmittedAt = null;
        guestSession.GroupReservationId = groupReservation.GroupReservationId;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockGroupReservationRepository
           .Setup(r => r.GetGroupReservationByIdAsync(guestSession.GroupReservationId))
           .ReturnsAsync(groupReservation);

        _mockDishRepository
            .Setup(d => d.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(dishes);

        GuestSession? session = null;
        _mockGuestSessionRepository
            .Setup(r => r.SaveAsync(It.IsAny<GuestSession>()))
            .Callback<GuestSession>(s => session = s);

        DateTime before = DateTime.UtcNow;
        await _guestSessionService.SubmitCartAsync(guestSession.Id);
        DateTime after = DateTime.UtcNow;

        Assert.NotNull(session);
        Assert.True(session.IsSubmitted);
        Assert.NotNull(session.SubmittedAt);
        Assert.InRange(session.SubmittedAt.Value, before, after);
    }

    [Theory]
    [AutoData]
    public async Task SubmitCartAsync_SessionNotFound_ThrowsGuestSessionNotFound(string guestSessionId)
    {
        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSessionId))
            .ReturnsAsync((GuestSession?)null);

        await Assert.ThrowsAsync<GuestSessionNotFoundException>(() => _guestSessionService.SubmitCartAsync(guestSessionId));
    }

    [Theory]
    [InlineAutoData(GroupReservationStatus.Submitted, typeof(GroupReservationSubmittedException))]
    [InlineAutoData(GroupReservationStatus.Closed, typeof(GroupReservationExpiredException))]
    public async Task SubmitCartAsync_GroupReservationNotOpen_ThrowsException(GroupReservationStatus status, Type exceptionType, GuestSession guestSession, GroupReservation groupReservation)
    {
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = status;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(guestSession.GroupReservationId))
            .ReturnsAsync(groupReservation);

        await Assert.ThrowsAsync(exceptionType, () => _guestSessionService.SubmitCartAsync(guestSession.Id));
    }

    [Theory]
    [AutoData]
    public async Task SubmitCartAsync_GuestSessionHasNoDishes_ThrowsCartIsEmptyException(GuestSession guestSession, GroupReservation groupReservation)
    {
        guestSession.Dishes = [];
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(guestSession.GroupReservationId))
            .ReturnsAsync(groupReservation);

        await Assert.ThrowsAsync<CartIsEmptyException>(() => _guestSessionService.SubmitCartAsync(guestSession.Id));
    }

    [Theory]
    [AutoData]
    public async Task SubmitCartAsync_GuestSessionSubmitted_ThrowsAlreadySubmited(GuestSession guestSession, GroupReservation groupReservation)
    {
        guestSession.IsSubmitted = true;
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.Status = GroupReservationStatus.Open;

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(guestSession.GroupReservationId))
            .ReturnsAsync(groupReservation);

        await Assert.ThrowsAsync<GuestSessionSubmittedException>(() => _guestSessionService.SubmitCartAsync(guestSession.Id));
    }

    [Theory]
    [InlineAutoData(DishState.OutOfStock)]
    [InlineAutoData(DishState.OnStop)]
    public async Task SubmitCartAsync_DishNotAvailable_ThrowsDishNotAvailable(DishState state, Dish dish, GuestSession guestSession, GroupReservation groupReservation)
    {
        dish.State = state;
        guestSession.IsSubmitted = false;
        guestSession.GroupReservationId = groupReservation.GroupReservationId;
        guestSession.Dishes = 
        [
            new OrderItem()
            {
                DishId = dish.Id,
                Quantity = 1,
            }
        ];

        _mockDishRepository
            .Setup(d => d.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([dish]);

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(guestSession.Id))
            .ReturnsAsync(guestSession);

        _mockGroupReservationRepository
           .Setup(r => r.GetGroupReservationByIdAsync(guestSession.GroupReservationId))
           .ReturnsAsync(groupReservation);

        await Assert.ThrowsAsync<DishIsNotAvailableException>(() => _guestSessionService.SubmitCartAsync(guestSession.Id));
    }
    [Theory]
    [MemberAutoData(nameof(PepareDishesForGuestSession))]
    public async Task GetGuestSessionInfoAsync_Guest_ReturnsAsync(
        IEnumerable<string> dishIds,
        IEnumerable<Dish> dishes,
        List<OrderItem> orderItems,
        decimal expectedTotal,
        GuestSession session)
    {
        session.Dishes = orderItems;
        session.IsOwnerSession = false;

        _mockDishRepository
            .Setup(r => r.GetDishesByIdsAsync(dishIds))
            .ReturnsAsync(dishes);

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(session.Id))
            .ReturnsAsync(session);

        GetGuestSessionResponse result = await _guestSessionService.GetGuestSessionInfoAsync(session.Id);

        Assert.Equal(session.GuestName, result.Name);
        Assert.Equal(dishes.Count(), result.Items.Count());
        Assert.Equal(expectedTotal, result.Total);
    }

    [Theory]
    [MemberAutoData(nameof(PepareDishesForGuestSession))]
    public async Task GetGuestSessionInfoAsync_GroupOwner_ReturnsAsync(
        IEnumerable<string> dishIds,
        IEnumerable<Dish> dishes,
        List<OrderItem> orderItems,
        decimal expectedTotal,
        GuestSession session,
        GroupReservation groupReservation,
        Reservation reservation,
        string customerId,
        string ownerName,
        UserType userType)
    {
        session.Dishes = orderItems;
        session.IsOwnerSession = true;
        session.GroupReservationId = groupReservation.GroupReservationId;
        groupReservation.ReservationId = reservation.Id;
        reservation.CustomerId = customerId;
        userType.Attributes.Add(new AttributeType()
        {
            Name = "given_name",
            Value = ownerName,
        });

        _mockDishRepository
            .Setup(r => r.GetDishesByIdsAsync(dishIds))
            .ReturnsAsync(dishes);

        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(session.Id))
            .ReturnsAsync(session);

        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(groupReservation.GroupReservationId))
            .ReturnsAsync(groupReservation);

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(customerId))
            .ReturnsAsync(userType);

        GetGuestSessionResponse result = await _guestSessionService.GetGuestSessionInfoAsync(session.Id);

        Assert.Equal(ownerName, result.Name);
        Assert.Equal(dishes.Count(), result.Items.Count());
        Assert.Equal(expectedTotal, result.Total);
    }

    private const string GroupOwnerUserId = "owner-user-1";
    private const string GroupId = "group-1";
    private const string ParentReservationId = "reservation-1";

    private static readonly Dish CaesarSalad = new() { Id = "dish1", Name = "Caesar Salad", Price = 180 };
    private static readonly Dish TomYum = new() { Id = "dish2", Name = "Tom Yum", Price = 220 };

    private void ArrangeGroupOwnedBy(string? customerId, GroupReservationStatus status = GroupReservationStatus.Open)
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync(new GroupReservation
            {
                GroupReservationId = GroupId,
                ReservationId = ParentReservationId,
                ReservationToken = "token-1",
                Status = status
            });

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ParentReservationId))
            .ReturnsAsync(new Reservation
            {
                Id = ParentReservationId,
                TableId = "table-1",
                WaiterId = "waiter-1",
                CustomerId = customerId
            });
    }

    private void ArrangeSessions(params GuestSession[] sessions)
    {
        _mockGuestSessionRepository
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupId))
            .ReturnsAsync(sessions.ToList());
    }

    private void ArrangeDishes(params Dish[] dishes)
    {
        _mockDishRepository
            .Setup(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(dishes);
    }

    private static GuestSession Session(string id, string name, bool isSubmitted = false, bool isOwnerSession = false, params (string DishId, int Quantity)[] items)
    {
        return new GuestSession
        {
            Id = id,
            GroupReservationId = GroupId,
            GuestName = name,
            IsSubmitted = isSubmitted,
            IsOwnerSession = isOwnerSession,
            Dishes = items.Select(i => new OrderItem { DishId = i.DishId, Quantity = i.Quantity }).ToList()
        };
    }

    [Fact]
    public async Task GetGroupSessionsAsync_UnknownGroup_ThrowsGroupReservationNotFoundException()
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(
            () => _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId));

        _mockGuestSessionRepository.Verify(r => r.GetAllByGroupReservationIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_ParentReservationMissing_ThrowsReservationNotFoundException()
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync(new GroupReservation
            {
                GroupReservationId = GroupId,
                ReservationId = ParentReservationId,
                ReservationToken = "token-1",
                Status = GroupReservationStatus.Open
            });
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ParentReservationId))
            .ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(
            () => _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId));

        _mockGuestSessionRepository.Verify(r => r.GetAllByGroupReservationIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_CallerIsNotOwner_ThrowsActionForbiddenException()
    {
        ArrangeGroupOwnedBy("another-user");
        ArrangeSessions(Session("session1", "Andrii", items: ("dish1", 1)));
        ArrangeDishes(CaesarSalad);

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId));

        _mockGuestSessionRepository.Verify(r => r.GetAllByGroupReservationIdAsync(It.IsAny<string>()), Times.Never);
        _mockDishRepository.Verify(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_ReservationHasNoCustomer_ThrowsActionForbiddenException()
    {
        ArrangeGroupOwnedBy(null);
        ArrangeSessions(Session("session1", "Andrii", items: ("dish1", 1)));
        ArrangeDishes(CaesarSalad);

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId));

        _mockGuestSessionRepository.Verify(r => r.GetAllByGroupReservationIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_OwnerWithGuestSessions_ReturnsParticipantsWithItemsAndTotals()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(
            Session("session1", "Andrii", isSubmitted: true, items: ("dish1", 2)),
            Session("session2", "Olena", isSubmitted: false, items: ("dish2", 1)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        var participants = result.Participants.ToList();
        Assert.Equal(2, participants.Count);

        var andrii = participants[0];
        Assert.Equal("session1", andrii.Id);
        Assert.Equal(ParticipantType.Guest, andrii.Type);
        Assert.Equal("Andrii", andrii.Name);
        var andriiItem = Assert.Single(andrii.Items);
        Assert.Equal("dish1", andriiItem.Dish.Id);
        Assert.Equal("Caesar Salad", andriiItem.Dish.Name);
        Assert.Equal(180m, andriiItem.Dish.Price);
        Assert.Equal(2, andriiItem.Quantity);
        Assert.Equal(360m, andrii.Total);
        Assert.True(andrii.IsSubmitted);

        var olena = participants[1];
        Assert.Equal("session2", olena.Id);
        Assert.Equal(ParticipantType.Guest, olena.Type);
        Assert.Equal("Olena", olena.Name);
        var olenaItem = Assert.Single(olena.Items);
        Assert.Equal("dish2", olenaItem.Dish.Id);
        Assert.Equal("Tom Yum", olenaItem.Dish.Name);
        Assert.Equal(220m, olenaItem.Dish.Price);
        Assert.Equal(1, olenaItem.Quantity);
        Assert.Equal(220m, olena.Total);
        Assert.False(olena.IsSubmitted);

        Assert.Equal(580m, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_OwnerHasOrder_ReturnsOwnerParticipantWithItemsAndTotal()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(Session("session1", "Andrii", isSubmitted: true, items: ("dish1", 1)));
        ArrangeDishes(CaesarSalad, TomYum);
        ArrangeOrder(
            DateTime.UtcNow,
            new OrderItem { DishId = "dish1", Quantity = 2 },
            new OrderItem { DishId = "dish2", Quantity = 1 });

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        Assert.NotNull(result.Owner);
        Assert.Equal($"owner-{GroupId}", result.Owner.Id);
        Assert.Equal(ParticipantType.Owner, result.Owner.Type);
        Assert.Equal(["dish1", "dish2"], result.Owner.Items.Select(i => i.Dish.Id));
        Assert.Equal([2, 1], result.Owner.Items.Select(i => i.Quantity));
        Assert.Equal(580m, result.Owner.Total);
        Assert.True(result.Owner.IsSubmitted);
        Assert.Single(result.Participants);
        Assert.Equal(180m, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_NoOwnerOrder_ReturnsEmptyNotSubmittedOwner()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(Session("session1", "Andrii", items: ("dish1", 1)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        Assert.NotNull(result.Owner);
        Assert.Equal(ParticipantType.Owner, result.Owner.Type);
        Assert.Empty(result.Owner.Items);
        Assert.Equal(0m, result.Owner.Total);
        Assert.False(result.Owner.IsSubmitted);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_SameDishInSeveralSessions_LoadsDistinctDishIdsInOneCall()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(
            Session("session1", "Andrii", items: ("dish1", 2)),
            Session("session2", "Olena", items: [("dish1", 1), ("dish2", 1)]));
        ArrangeDishes(CaesarSalad, TomYum);

        await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        _mockDishRepository.Verify(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
        _mockDishRepository.Verify(r => r.GetDishesByIdsAsync(It.Is<IEnumerable<string>>(ids =>
            ids.Count() == ids.Distinct().Count()
            && ids.ToHashSet().SetEquals(new[] { "dish1", "dish2" }))), Times.Once);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_NoSessions_ReturnsEmptyParticipantsAndZeroTotal()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions();
        ArrangeDishes();

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        Assert.NotNull(result);
        Assert.Empty(result.Participants);
        Assert.Equal(0m, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_SessionWithNoDishes_ListsParticipantWithEmptyItemsAndZeroTotal()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(Session("session1", "Andrii"));
        ArrangeDishes();

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        var participant = Assert.Single(result.Participants);
        Assert.Equal("session1", participant.Id);
        Assert.Equal("Andrii", participant.Name);
        Assert.Empty(participant.Items);
        Assert.Equal(0m, participant.Total);
        Assert.Equal(0m, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_OwnerSessionPresent_ReturnsOnlyGuestSessions()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(
            Session("owner-session", "Owner", isOwnerSession: true, items: ("dish2", 3)),
            Session("session1", "Andrii", items: ("dish1", 2)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        var participant = Assert.Single(result.Participants);
        Assert.Equal("session1", participant.Id);
        Assert.Equal(ParticipantType.Guest, participant.Type);
        Assert.Equal(360m, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_ItemWithUnknownDish_SkipsItemAndExcludesItFromTotals()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(Session("session1", "Andrii", items: [("dish1", 2), ("deleted-dish", 5)]));
        ArrangeDishes(CaesarSalad);

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        var participant = Assert.Single(result.Participants);
        var item = Assert.Single(participant.Items);
        Assert.Equal("dish1", item.Dish.Id);
        Assert.Equal(360m, participant.Total);
        Assert.Equal(360m, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_SessionWithSeveralItems_KeepsSessionDishesOrder()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(Session("session1", "Andrii", items: [("dish2", 1), ("dish1", 2)]));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        var participant = Assert.Single(result.Participants);
        var items = participant.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("dish2", items[0].Dish.Id);
        Assert.Equal(1, items[0].Quantity);
        Assert.Equal("dish1", items[1].Dish.Id);
        Assert.Equal(2, items[1].Quantity);
        Assert.Equal(580m, participant.Total);
    }

    [Theory]
    [InlineData(GroupReservationStatus.Open)]
    [InlineData(GroupReservationStatus.Submitted)]
    [InlineData(GroupReservationStatus.Closed)]
    public async Task GetGroupSessionsAsync_AnyGroupStatus_ReturnsSessions(GroupReservationStatus status)
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId, status);
        ArrangeSessions(Session("session1", "Andrii", items: ("dish1", 1)));
        ArrangeDishes(CaesarSalad);

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        var participant = Assert.Single(result.Participants);
        Assert.Equal("session1", participant.Id);
        Assert.Equal(180m, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_HappyPath_DoesNotModifySessions()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessions(
            Session("session1", "Andrii", isSubmitted: true, items: ("dish1", 2)),
            Session("session2", "Olena", items: ("dish2", 1)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.GetGroupSessionsAsync(GroupOwnerUserId, GroupId);

        Assert.Equal(2, result.Participants.Count());
        _mockGuestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
        _mockGuestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    private const string OwnerSessionId = "owner-group-1";

    private void ArrangeReservationOwnedBy(string? customerId)
    {
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ParentReservationId))
            .ReturnsAsync(new Reservation
            {
                Id = ParentReservationId,
                TableId = "table-1",
                WaiterId = "waiter-1",
                CustomerId = customerId
            });
    }

    private void ArrangeGroupOfReservation(GroupReservationStatus status = GroupReservationStatus.Open)
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetByReservationIdAsync(ParentReservationId))
            .ReturnsAsync(new GroupReservation
            {
                GroupReservationId = GroupId,
                ReservationId = ParentReservationId,
                ReservationToken = "token-1",
                Status = status
            });
    }

    private void ArrangeExistingOwnerSession()
    {
        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(OwnerSessionId))
            .ReturnsAsync(Session(OwnerSessionId, "Owner", isOwnerSession: true));
    }

    private void VerifyNoOwnerSessionWrite()
    {
        _mockGuestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateOwnerSessionIdAsync_ReservationNotFound_ThrowsReservationNotFoundException()
    {
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ParentReservationId))
            .ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(
            () => _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId));

        _mockGroupReservationRepository.Verify(r => r.GetByReservationIdAsync(It.IsAny<string>()), Times.Never);
        _mockGuestSessionRepository.Verify(r => r.GetByGuestSessionId(It.IsAny<string>()), Times.Never);
        VerifyNoOwnerSessionWrite();
    }

    [Theory]
    [InlineData("another-user")]
    [InlineData("OWNER-USER-1")]
    public async Task GetOrCreateOwnerSessionIdAsync_CallerIsNotOwner_ThrowsActionForbiddenException(string customerId)
    {
        ArrangeReservationOwnedBy(customerId);
        ArrangeGroupOfReservation();

        var exception = await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId));

        Assert.Equal("Only the reservation owner can perform this action.", exception.Message);
        _mockGroupReservationRepository.Verify(r => r.GetByReservationIdAsync(It.IsAny<string>()), Times.Never);
        _mockGuestSessionRepository.Verify(r => r.GetByGuestSessionId(It.IsAny<string>()), Times.Never);
        VerifyNoOwnerSessionWrite();
    }

    [Fact]
    public async Task GetOrCreateOwnerSessionIdAsync_ReservationHasNoCustomer_ThrowsActionForbiddenException()
    {
        ArrangeReservationOwnedBy(null);
        ArrangeGroupOfReservation();

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId));

        VerifyNoOwnerSessionWrite();
    }

    [Fact]
    public async Task GetOrCreateOwnerSessionIdAsync_NoGroupReservation_ThrowsGroupReservationNotFoundException()
    {
        ArrangeReservationOwnedBy(GroupOwnerUserId);
        _mockGroupReservationRepository
            .Setup(r => r.GetByReservationIdAsync(ParentReservationId))
            .ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(
            () => _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId));

        _mockGuestSessionRepository.Verify(r => r.GetByGuestSessionId(It.IsAny<string>()), Times.Never);
        VerifyNoOwnerSessionWrite();
    }

    [Fact]
    public async Task GetOrCreateOwnerSessionIdAsync_OwnerSessionExists_ReturnsItsIdWithoutSaving()
    {
        ArrangeReservationOwnedBy(GroupOwnerUserId);
        ArrangeGroupOfReservation();
        ArrangeExistingOwnerSession();

        OwnerSessionIdResponse result = await _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId);

        Assert.Equal(OwnerSessionId, result.GuestSessionId);
        VerifyNoOwnerSessionWrite();
    }

    [Fact]
    public async Task GetOrCreateOwnerSessionIdAsync_NoOwnerSession_SavesOwnerSessionAndReturnsItsId()
    {
        ArrangeReservationOwnedBy(GroupOwnerUserId);
        ArrangeGroupOfReservation();
        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(OwnerSessionId))
            .ReturnsAsync((GuestSession?)null);

        GuestSession? saved = null;
        _mockGuestSessionRepository
            .Setup(r => r.SaveAsync(It.IsAny<GuestSession>()))
            .Callback<GuestSession>(s => saved = s);

        OwnerSessionIdResponse result = await _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId);

        Assert.Equal(OwnerSessionId, result.GuestSessionId);
        _mockGuestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Once);
        Assert.NotNull(saved);
        Assert.Equal(OwnerSessionId, saved.Id);
        Assert.Equal(GroupId, saved.GroupReservationId);
        Assert.Equal("Owner", saved.GuestName);
        Assert.True(saved.IsOwnerSession);
        Assert.Equal(GroupOwnerUserId, saved.OwnerUserId);
        Assert.NotNull(saved.Dishes);
        Assert.Empty(saved.Dishes);
        Assert.False(saved.IsSubmitted);
        Assert.Null(saved.SessionTokenHash);
    }

    [Theory]
    [InlineData(GroupReservationStatus.Open)]
    [InlineData(GroupReservationStatus.Submitted)]
    [InlineData(GroupReservationStatus.Closed)]
    public async Task GetOrCreateOwnerSessionIdAsync_AnyGroupStatusWithoutOwnerSession_CreatesOwnerSession(GroupReservationStatus status)
    {
        ArrangeReservationOwnedBy(GroupOwnerUserId);
        ArrangeGroupOfReservation(status);

        OwnerSessionIdResponse result = await _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId);

        Assert.Equal(OwnerSessionId, result.GuestSessionId);
        _mockGuestSessionRepository.Verify(
            r => r.SaveAsync(It.Is<GuestSession>(s => s.Id == OwnerSessionId && s.IsOwnerSession)),
            Times.Once);
    }

    [Theory]
    [InlineData(GroupReservationStatus.Open)]
    [InlineData(GroupReservationStatus.Submitted)]
    [InlineData(GroupReservationStatus.Closed)]
    public async Task GetOrCreateOwnerSessionIdAsync_AnyGroupStatusWithOwnerSession_ReturnsItsIdWithoutSaving(GroupReservationStatus status)
    {
        ArrangeReservationOwnedBy(GroupOwnerUserId);
        ArrangeGroupOfReservation(status);
        ArrangeExistingOwnerSession();

        OwnerSessionIdResponse result = await _guestSessionService.GetOrCreateOwnerSessionIdAsync(GroupOwnerUserId, ParentReservationId);

        Assert.Equal(OwnerSessionId, result.GuestSessionId);
        VerifyNoOwnerSessionWrite();
    }

    public static TheoryData<IEnumerable<string>, IEnumerable<Dish>, List<OrderItem>, decimal> PepareDishesForGuestSession()
    {
        var dishMap = new Dictionary<string, Dish>()
        {
            ["1"] = new()
            {
                Id = "1",
                Price = 1
            },
            ["2"] = new()
            {
                Id = "2",
                Price = 2
            },
            ["3"] = new()
            {
                Id = "3",
                Price = 3
            }
        };

        var orderItems = new List<OrderItem>
        {
            new OrderItem()
                    {
                        DishId = "1",
                        Quantity = 2
                    },
                    new OrderItem()
                    {
                        DishId = "2",
                        Quantity = 3
                    },
                    new OrderItem()
                    {
                        DishId = "3",
                        Quantity = 1
                    }
        };

        var expectedTotal = orderItems.Sum(o => dishMap[o.DishId].Price * o.Quantity);

        return new()
        {
            { dishMap.Keys, dishMap.Values, orderItems, expectedTotal }
        };
    }

    #region DeleteGuestSessionAsync

    private const string DeletedSessionId = "session1";

    private void ArrangeSessionById(GuestSession session)
    {
        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(session.Id))
            .ReturnsAsync(session);
    }

    private void VerifyNoDelete()
    {
        _mockGuestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    private void VerifyNoSessionLookup()
    {
        _mockGuestSessionRepository.Verify(r => r.GetByGuestSessionId(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_UnknownGroup_ThrowsGroupReservationNotFoundException()
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync((GroupReservation?)null);
        ArrangeSessionById(Session(DeletedSessionId, "Andrii"));

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoSessionLookup();
        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_ParentReservationMissing_ThrowsReservationNotFoundException()
    {
        _mockGroupReservationRepository
            .Setup(r => r.GetGroupReservationByIdAsync(GroupId))
            .ReturnsAsync(new GroupReservation
            {
                GroupReservationId = GroupId,
                ReservationId = ParentReservationId,
                ReservationToken = "token-1",
                Status = GroupReservationStatus.Open
            });
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(ParentReservationId))
            .ReturnsAsync((Reservation?)null);
        ArrangeSessionById(Session(DeletedSessionId, "Andrii"));

        await Assert.ThrowsAsync<ReservationNotFoundException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_CallerIsNotOwner_ThrowsActionForbiddenException()
    {
        ArrangeGroupOwnedBy("another-user");
        ArrangeSessionById(Session(DeletedSessionId, "Andrii"));

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoSessionLookup();
        VerifyNoDelete();
    }

    [Theory]
    [InlineData(GroupReservationStatus.Submitted)]
    [InlineData(GroupReservationStatus.Closed)]
    public async Task DeleteGuestSessionAsync_CallerIsNotOwnerAndGroupNotOpen_ThrowsActionForbiddenException(GroupReservationStatus status)
    {
        ArrangeGroupOwnedBy("another-user", status);
        ArrangeSessionById(Session(DeletedSessionId, "Andrii"));

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoSessionLookup();
        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_ReservationHasNoCustomer_ThrowsActionForbiddenException()
    {
        ArrangeGroupOwnedBy(null);
        ArrangeSessionById(Session(DeletedSessionId, "Andrii"));

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_GroupSubmitted_ThrowsGroupReservationSubmittedException()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId, GroupReservationStatus.Submitted);
        ArrangeSessionById(Session(DeletedSessionId, "Andrii"));
        ArrangeSessions();
        ArrangeDishes();

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoSessionLookup();
        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_GroupClosed_ThrowsGroupReservationExpiredException()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId, GroupReservationStatus.Closed);
        ArrangeSessionById(Session(DeletedSessionId, "Andrii"));
        ArrangeSessions();
        ArrangeDishes();

        await Assert.ThrowsAsync<GroupReservationExpiredException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoSessionLookup();
        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionNotFound_ThrowsGuestSessionNotFoundException()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(DeletedSessionId))
            .ReturnsAsync((GuestSession?)null);

        await Assert.ThrowsAsync<GuestSessionNotFoundException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionOfAnotherGroup_ThrowsSameGuestSessionNotFoundAsMissingSession()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        _mockGuestSessionRepository
            .Setup(r => r.GetByGuestSessionId(DeletedSessionId))
            .ReturnsAsync((GuestSession?)null);
        var missing = await Assert.ThrowsAsync<GuestSessionNotFoundException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        var otherGroupSession = Session(DeletedSessionId, "Andrii", items: ("dish1", 1));
        otherGroupSession.GroupReservationId = "group-2";
        ArrangeSessionById(otherGroupSession);
        ArrangeSessions();
        ArrangeDishes();

        var otherGroup = await Assert.ThrowsAsync<GuestSessionNotFoundException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId));

        Assert.Equal(missing.Message, otherGroup.Message);
        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_OwnerSession_ThrowsOwnerSessionNotDeletableException()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessionById(Session("owner-group-1", "Owner", isOwnerSession: true));
        ArrangeSessions();
        ArrangeDishes();

        await Assert.ThrowsAsync<OwnerSessionNotDeletableException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, "owner-group-1"));

        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_OwnerSessionAndCallerIsNotOwner_ThrowsActionForbiddenException()
    {
        ArrangeGroupOwnedBy("another-user");
        ArrangeSessionById(Session("owner-group-1", "Owner", isOwnerSession: true));

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, "owner-group-1"));

        VerifyNoDelete();
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_OwnerSessionOfAnotherGroup_ThrowsGuestSessionNotFoundException()
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        var otherOwnerSession = Session("owner-group-2", "Owner", isOwnerSession: true);
        otherOwnerSession.GroupReservationId = "group-2";
        ArrangeSessionById(otherOwnerSession);
        ArrangeSessions();
        ArrangeDishes();

        await Assert.ThrowsAsync<GuestSessionNotFoundException>(
            () => _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, "owner-group-2"));

        VerifyNoDelete();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteGuestSessionAsync_OpenGroupGuestSession_DeletesRequestedSessionOnce(bool isSubmitted)
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        var loadedSession = Session(DeletedSessionId, "Andrii", isSubmitted: isSubmitted, items: ("dish1", 2));
        ArrangeSessionById(loadedSession);
        ArrangeSessions();
        ArrangeDishes();

        await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        _mockGuestSessionRepository.Verify(
            r => r.DeleteAsync(It.Is<GuestSession>(s => ReferenceEquals(s, loadedSession))),
            Times.Once);
        _mockGuestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Once);
    }

    #endregion

    #region DeleteGuestSessionAsync response

    private void ArrangeDeletableGuest(GuestSession session)
    {
        ArrangeGroupOwnedBy(GroupOwnerUserId);
        ArrangeSessionById(session);
    }

    private static GuestSession Andrii() => Session(DeletedSessionId, "Andrii", items: ("dish1", 2));

    [Fact]
    public async Task DeleteGuestSessionAsync_TaskExample_ReturnsRemainingGuestWithItemsAndTotals()
    {
        ArrangeDeletableGuest(Andrii());
        ArrangeSessions(Session("session2", "Olena", isSubmitted: false, items: ("dish2", 1)));
        ArrangeDishes(TomYum);

        GetAllSessionsResponse result = await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        var olena = Assert.Single(result.Participants);
        Assert.Equal("session2", olena.Id);
        Assert.Equal(ParticipantType.Guest, olena.Type);
        Assert.Equal("Olena", olena.Name);
        var item = Assert.Single(olena.Items);
        Assert.Equal("dish2", item.Dish.Id);
        Assert.Equal("Tom Yum", item.Dish.Name);
        Assert.Equal(220m, item.Dish.Price);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(220m, olena.Total);
        Assert.False(olena.IsSubmitted);
        Assert.Equal(220m, result.Total);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_RequeryStillReturnsDeletedSession_ExcludesItFromParticipantsAndTotal()
    {
        ArrangeDeletableGuest(Andrii());
        ArrangeSessions(
            Andrii(),
            Session("session2", "Olena", items: ("dish2", 1)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        var participant = Assert.Single(result.Participants);
        Assert.Equal("session2", participant.Id);
        Assert.Equal(220m, result.Total);
        _mockDishRepository.Verify(r => r.GetDishesByIdsAsync(It.Is<IEnumerable<string>>(ids =>
            ids.ToHashSet().SetEquals(new[] { "dish2" }))), Times.Once);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_RequeryReturnsOwnerSession_ExcludesOwnerFromParticipantsTotalAndDishLookup()
    {
        ArrangeDeletableGuest(Andrii());
        ArrangeSessions(
            Session("owner-group-1", "Owner", isOwnerSession: true, items: ("dish1", 3)),
            Session("session2", "Olena", items: ("dish2", 1)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        var participant = Assert.Single(result.Participants);
        Assert.Equal("session2", participant.Id);
        Assert.Equal(ParticipantType.Guest, participant.Type);
        Assert.Equal(220m, result.Total);
        _mockDishRepository.Verify(r => r.GetDishesByIdsAsync(It.Is<IEnumerable<string>>(ids =>
            ids.ToHashSet().SetEquals(new[] { "dish2" }))), Times.Once);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_ItemWithUnknownDish_SkipsItemAndExcludesItFromTotals()
    {
        ArrangeDeletableGuest(Andrii());
        ArrangeSessions(Session("session2", "Olena", items: [("dish-removed", 4), ("dish2", 1)]));
        ArrangeDishes(TomYum);

        GetAllSessionsResponse result = await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        var participant = Assert.Single(result.Participants);
        var item = Assert.Single(participant.Items);
        Assert.Equal("dish2", item.Dish.Id);
        Assert.Equal(220m, participant.Total);
        Assert.Equal(220m, result.Total);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SameDishInSeveralRemainingSessions_LoadsDistinctDishIdsInOneCall()
    {
        ArrangeDeletableGuest(Andrii());
        ArrangeSessions(
            Session("session2", "Olena", items: [("dish1", 1), ("dish2", 1)]),
            Session("session3", "Taras", items: ("dish1", 2)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        _mockDishRepository.Verify(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
        _mockDishRepository.Verify(r => r.GetDishesByIdsAsync(It.Is<IEnumerable<string>>(ids =>
            ids.Count() == ids.Distinct().Count()
            && ids.ToHashSet().SetEquals(new[] { "dish1", "dish2" }))), Times.Once);
        Assert.Equal(400m + 360m, result.Total);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteGuestSessionAsync_LastGuestDeleted_ReturnsEmptyParticipantsAndZeroTotal(bool ownerSessionRemains)
    {
        ArrangeDeletableGuest(Andrii());
        if (ownerSessionRemains)
        {
            ArrangeSessions(Session("owner-group-1", "Owner", isOwnerSession: true, items: ("dish2", 2)));
        }
        else
        {
            ArrangeSessions();
        }
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        Assert.NotNull(result);
        Assert.Empty(result.Participants);
        Assert.Equal(0m, result.Total);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_RemainingGuests_KeepsRepositoryOrderGuestTypeAndSubmittedFlag()
    {
        ArrangeDeletableGuest(Andrii());
        ArrangeSessions(
            Session("session3", "Taras", isSubmitted: true, items: ("dish1", 1)),
            Session("session2", "Olena", isSubmitted: false, items: ("dish2", 1)));
        ArrangeDishes(CaesarSalad, TomYum);

        GetAllSessionsResponse result = await _guestSessionService.DeleteGuestSessionAsync(GroupOwnerUserId, GroupId, DeletedSessionId);

        var participants = result.Participants.ToList();
        Assert.Equal(["session3", "session2"], participants.Select(p => p.Id));
        Assert.All(participants, p => Assert.Equal(ParticipantType.Guest, p.Type));
        Assert.True(participants[0].IsSubmitted);
        Assert.False(participants[1].IsSubmitted);
        Assert.Equal(400m, result.Total);
    }

    #endregion
}





