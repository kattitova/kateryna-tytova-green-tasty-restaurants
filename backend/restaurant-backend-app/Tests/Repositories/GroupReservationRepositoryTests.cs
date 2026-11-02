using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Repositories.GroupReservations;
using Moq;
using System.Text.RegularExpressions;

namespace APIHandler.Tests.Repositories;

public class GroupReservationRepositoryTests
{
    private const string GroupId = "group-1";
    private const string GroupTableName = "test-group-reservations-table";
    private const string OrdersTableName = "test-orders-table";

    private readonly Mock<IDynamoDBContext> _mockContext = new();
    private readonly Mock<ITransactWrite<Order>> _mockOrderPart = new();
    private readonly Mock<ITransactWrite<GroupReservation>> _mockGroupPart = new();
    private readonly Mock<IMultiTableTransactWrite> _mockTransaction = new();

    private TransactWriteConfig? _orderPartConfig;
    private TransactWriteConfig? _groupPartConfig;
    private ITransactWrite[]? _transactionParts;
    private object? _groupHashKey;
    private Expression? _updateExpression;
    private Expression? _conditionExpression;

    private readonly GroupReservationRepository _repository;

    public GroupReservationRepositoryTests()
    {
        // The repository reads the table names from env variables (one of them in a static field),
        // so set them before the repository type is used.
        Environment.SetEnvironmentVariable("groupReservationsTable", GroupTableName);
        Environment.SetEnvironmentVariable("ordersTable", OrdersTableName);

        _mockContext
            .Setup(c => c.CreateTransactWrite<Order>(It.IsAny<TransactWriteConfig>()))
            .Callback<TransactWriteConfig>(config => _orderPartConfig = config)
            .Returns(_mockOrderPart.Object);

        _mockContext
            .Setup(c => c.CreateTransactWrite<GroupReservation>(It.IsAny<TransactWriteConfig>()))
            .Callback<TransactWriteConfig>(config => _groupPartConfig = config)
            .Returns(_mockGroupPart.Object);

        _mockContext
            .Setup(c => c.CreateMultiTableTransactWrite(It.IsAny<ITransactWrite[]>()))
            .Callback<ITransactWrite[]>(parts => _transactionParts = parts)
            .Returns(_mockTransaction.Object);

        _mockGroupPart
            .Setup(p => p.AddSaveItem(It.IsAny<object>(), It.IsAny<Expression>(), It.IsAny<Expression>()))
            .Callback<object, Expression, Expression>((hashKey, update, condition) =>
            {
                _groupHashKey = hashKey;
                _updateExpression = update;
                _conditionExpression = condition;
            });

        _repository = new GroupReservationRepository(_mockContext.Object);
    }

    private static Order SampleOrder() => new()
    {
        OrderId = "order-1",
        CustomerId = "owner-user-1",
        ReservationId = "reservation-1",
        ReservationAddress = "Address 1",
        ReservationTime = "12:00",
        Dishes = [new OrderItem { DishId = "dish1", Quantity = 3, State = OrderItemState.Pending }],
        Status = OrderStatus.SubmittedByCustomer
    };

    private void ArrangeExecuteSucceeds()
    {
        _mockTransaction
            .Setup(t => t.ExecuteAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void ArrangeExecuteThrows(Exception exception)
    {
        _mockTransaction
            .Setup(t => t.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
    }

    // Reasons are in transaction item order: index 0 = order item, index 1 = group item.
    private static TransactionCanceledException Canceled(params string[] reasonCodes) =>
        new("Transaction cancelled.")
        {
            CancellationReasons = reasonCodes.Select(code => new CancellationReason { Code = code }).ToList()
        };

    // Replaces the #name and :value placeholders with the real attribute name and number,
    // and collapses whitespace. Every value must be a DynamoDB number (N).
    private static string Resolve(Expression expression)
    {
        string statement = expression.ExpressionStatement;

        foreach (var (placeholder, name) in (expression.ExpressionAttributeNames ?? [])
                     .OrderByDescending(p => p.Key.Length))
        {
            statement = statement.Replace(placeholder, name);
        }

        foreach (var (placeholder, entry) in (expression.ExpressionAttributeValues ?? [])
                     .OrderByDescending(p => p.Key.Length))
        {
            var primitive = Assert.IsType<Primitive>(entry);
            Assert.Equal(DynamoDBEntryType.Numeric, primitive.Type);
            statement = statement.Replace(placeholder, primitive.Value.ToString());
        }

        return Regex.Replace(statement.Trim(), @"\s+", " ");
    }

    // Success

    [Fact]
    public async Task SubmitGroupPreOrderAsync_TransactionSucceeds_ReturnsTrue()
    {
        ArrangeExecuteSucceeds();

        bool result = await _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder());

        Assert.True(result);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_TransactionSucceeds_PutsTheSameOrderOnceInOrdersTable()
    {
        ArrangeExecuteSucceeds();
        Order order = SampleOrder();

        await _repository.SubmitGroupPreOrderAsync(GroupId, order);

        _mockOrderPart.Verify(p => p.AddSaveItem(order), Times.Once);
        _mockOrderPart.Verify(p => p.AddSaveItem(It.Is<Order>(o => !ReferenceEquals(o, order))), Times.Never);
        Assert.NotNull(_orderPartConfig);
        Assert.Equal(OrdersTableName, _orderPartConfig.OverrideTableName);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_TransactionSucceeds_UpdatesGroupByHashKeyOnceInGroupTable()
    {
        ArrangeExecuteSucceeds();

        await _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder());

        _mockGroupPart.Verify(
            p => p.AddSaveItem(It.IsAny<object>(), It.IsAny<Expression>(), It.IsAny<Expression>()), Times.Once);
        _mockGroupPart.Verify(p => p.AddSaveItem(It.IsAny<GroupReservation>()), Times.Never);
        Assert.Equal(GroupId, _groupHashKey);
        Assert.NotNull(_groupPartConfig);
        Assert.Equal(GroupTableName, _groupPartConfig.OverrideTableName);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_TransactionSucceeds_UpdateSetsStatusToSubmittedNumber()
    {
        ArrangeExecuteSucceeds();

        await _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder());

        Assert.NotNull(_updateExpression);
        Assert.Contains("status", _updateExpression.ExpressionAttributeNames.Values);
        Assert.Equal("SET status = 1", Resolve(_updateExpression), ignoreCase: true);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_TransactionSucceeds_ConditionRequiresStatusOpenNumber()
    {
        ArrangeExecuteSucceeds();

        await _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder());

        Assert.NotNull(_conditionExpression);
        Assert.Contains("status", _conditionExpression.ExpressionAttributeNames.Values);
        Assert.Equal("status = 0", Resolve(_conditionExpression), ignoreCase: true);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_TransactionSucceeds_ExecutesOneTransactionWithOrderPartThenGroupPart()
    {
        ArrangeExecuteSucceeds();

        await _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder());

        _mockContext.Verify(c => c.CreateMultiTableTransactWrite(It.IsAny<ITransactWrite[]>()), Times.Once);
        Assert.NotNull(_transactionParts);
        Assert.Equal(2, _transactionParts.Length);
        Assert.Same(_mockOrderPart.Object, _transactionParts[0]);
        Assert.Same(_mockGroupPart.Object, _transactionParts[1]);
        _mockTransaction.Verify(t => t.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Condition failure on the group item (F3)

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupItemConditionCheckFailed_ReturnsFalse()
    {
        ArrangeExecuteThrows(Canceled("None", "ConditionalCheckFailed"));

        bool result = await _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder());

        Assert.False(result);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ConditionCheckFailedOnOrderItemOnly_Propagates()
    {
        var exception = Canceled("ConditionalCheckFailed", "None");
        ArrangeExecuteThrows(exception);

        var thrown = await Assert.ThrowsAsync<TransactionCanceledException>(
            () => _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder()));

        Assert.Same(exception, thrown);
    }

    public static TheoryData<string[]> NonGroupConditionReasons() => new()
    {
        new[] { "TransactionConflict" },
        new[] { "None", "TransactionConflict" },
        new[] { "TransactionConflict", "None" },
        new[] { "None", "ThrottlingError" },
    };

    [Theory]
    [MemberData(nameof(NonGroupConditionReasons))]
    public async Task SubmitGroupPreOrderAsync_GroupItemReasonIsNotConditionCheckFailed_Propagates(string[] reasonCodes)
    {
        var exception = Canceled(reasonCodes);
        ArrangeExecuteThrows(exception);

        var thrown = await Assert.ThrowsAsync<TransactionCanceledException>(
            () => _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder()));

        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_CanceledWithoutReasons_Propagates()
    {
        var exception = new TransactionCanceledException("Transaction cancelled.");
        ArrangeExecuteThrows(exception);

        var thrown = await Assert.ThrowsAsync<TransactionCanceledException>(
            () => _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder()));

        Assert.Same(exception, thrown);
    }

    // Other exceptions (D13)

    [Fact]
    public async Task SubmitGroupPreOrderAsync_OtherDynamoDbException_Propagates()
    {
        var exception = new AmazonDynamoDBException("Service unavailable.");
        ArrangeExecuteThrows(exception);

        var thrown = await Assert.ThrowsAsync<AmazonDynamoDBException>(
            () => _repository.SubmitGroupPreOrderAsync(GroupId, SampleOrder()));

        Assert.Same(exception, thrown);
    }
}
