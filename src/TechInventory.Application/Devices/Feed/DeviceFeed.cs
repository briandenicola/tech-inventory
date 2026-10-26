using TechInventory.Application.Common.Paging;

namespace TechInventory.Application.Devices.Feed;

/// <summary>
/// One page of the read-only device feed the household life store reads in full on
/// each poll (spec 007 §3).
/// </summary>
/// <remarks>
/// Every device is listed whatever its status, so a disposed device keeps its history.
/// Reference ids are resolved to names so the reader needs no second call. Serial
/// number, IP address, MAC address, notes, product URL and audit actors are left out,
/// as for MCP (ADR 0004): the life store hands what it reads to agents.
/// </remarks>
public sealed record DeviceFeedResponse(
    string Schema,
    IReadOnlyList<DeviceFeedRecord> Records,
    string? Next)
{
    public const string SchemaName = "techinventory.lifestore.records/1";
}

public sealed record DeviceFeedRecord(
    string Kind,
    string Id,
    DateTimeOffset UpdatedAt,
    DeviceFeedItem Data);

public sealed record DeviceFeedItem(
    Guid Id,
    string Name,
    string? Model,
    string? Brand,
    string Category,
    string Owner,
    string Location,
    string? Network,
    IReadOnlyList<string> Tags,
    string Status,
    DateOnly? PurchaseDate,
    decimal? PurchasePrice,
    string CurrencyCode,
    DateOnly? WarrantyExpiry,
    DateOnly? RetiredDate,
    string? DisposalMethod,
    string? Purpose,
    string? OperatingSystem,
    string? Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>Reads feed pages: every device, oldest first, so a new device never shifts a page already read.</summary>
public interface IDeviceFeedReader
{
    Task<PagedResult<DeviceFeedItem>> ReadPageAsync(PageRequest pageRequest, CancellationToken cancellationToken);
}
