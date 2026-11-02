using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using APIHandler.Constants;
using APIHandler.Entities;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Repositories.GroupReservations;

public class GroupReservationRepository : IGroupReservationRepository
{
    private static readonly string GroupReservationsTableName = ResourceNames.GroupReservationsTableName;

    // Transaction item order in SubmitGroupPreOrderAsync: 0 = order, 1 = group.
    // TransactionCanceledException.CancellationReasons follow the same order.
    private const int GroupItemIndex = 1;
    private const string StatusAttributeName = "status";
    private const string ConditionalCheckFailed = "ConditionalCheckFailed";

    private readonly IDynamoDBContext _dynamoDbContext;

    public GroupReservationRepository(IDynamoDBContext dynamoDbContext)
    {
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
    }

    public async Task<GroupReservation?> GetGroupReservationByIdAsync(string groupReservationId)
    {
        return await _dynamoDbContext.LoadAsync<GroupReservation>(groupReservationId, DynamoConfig.LoadConfig(GroupReservationsTableName));
    }

    public async Task DeleteGroupReservationAsync(GroupReservation groupReservation)
    {
        await _dynamoDbContext.DeleteAsync(groupReservation, DynamoConfig.DeleteConfig(GroupReservationsTableName));
    }

    public async Task SaveGroupReservationAsync(GroupReservation groupReservation)
    {
        await _dynamoDbContext.SaveAsync(groupReservation, DynamoConfig.SaveConfig(GroupReservationsTableName));
    }

    public async Task<GroupReservation?> GetGroupReservationByTokenAsync(string token)
    {
        var results = await _dynamoDbContext.QueryAsync<GroupReservation?>(token, new QueryConfig
        {
            OverrideTableName = GroupReservationsTableName,
            IndexName = GlobalSecondaryIndexes.GroupReservation.ReservationToken,
        }).GetRemainingAsync();

        return results.FirstOrDefault();
    }

    public async Task<GroupReservation?> GetByReservationIdAsync(string reservationId)
    {
        var results = await _dynamoDbContext.QueryAsync<GroupReservation?>(reservationId, new QueryConfig
        {
            OverrideTableName = GroupReservationsTableName,
            IndexName = GlobalSecondaryIndexes.GroupReservation.ReservationId,
        }).GetRemainingAsync();

        return results.FirstOrDefault();
    }

    public async Task<bool> SubmitGroupPreOrderAsync(string groupReservationId, Order order)
    {
        ITransactWrite<Order> orderPart = _dynamoDbContext.CreateTransactWrite<Order>(
            new TransactWriteConfig { OverrideTableName = ResourceNames.OrdersTableName });
        orderPart.AddSaveItem(order);

        ITransactWrite<GroupReservation> groupPart = _dynamoDbContext.CreateTransactWrite<GroupReservation>(
            new TransactWriteConfig { OverrideTableName = GroupReservationsTableName });
        groupPart.AddSaveItem(groupReservationId, SetStatusSubmitted(), StatusIsOpen());

        try
        {
            await _dynamoDbContext.CreateMultiTableTransactWrite(orderPart, groupPart).ExecuteAsync();
            return true;
        }
        catch (TransactionCanceledException ex) when (IsGroupConditionFailure(ex))
        {
            return false;
        }
    }

    private static Expression SetStatusSubmitted() => new()
    {
        ExpressionStatement = "SET #status = :submitted",
        ExpressionAttributeNames = { ["#status"] = StatusAttributeName },
        ExpressionAttributeValues = { [":submitted"] = StatusNumber(GroupReservationStatus.Submitted) }
    };

    private static Expression StatusIsOpen() => new()
    {
        ExpressionStatement = "#status = :open",
        ExpressionAttributeNames = { ["#status"] = StatusAttributeName },
        ExpressionAttributeValues = { [":open"] = StatusNumber(GroupReservationStatus.Open) }
    };

    // Status has no converter, so the SDK stores it as a number (N). The expression values must match that type.
    private static Primitive StatusNumber(GroupReservationStatus status) =>
        new(((int)status).ToString(CultureInfo.InvariantCulture), saveAsNumeric: true);

    private static bool IsGroupConditionFailure(TransactionCanceledException ex) =>
        ex.CancellationReasons is { Count: > GroupItemIndex } reasons
        && reasons[GroupItemIndex].Code == ConditionalCheckFailed;
}
