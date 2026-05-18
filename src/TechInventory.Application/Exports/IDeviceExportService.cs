using TechInventory.Application.Abstractions.Repositories;

namespace TechInventory.Application.Exports;

public interface IDeviceExportService
{
    IAsyncEnumerable<DeviceExportRow> StreamExportAsync(DeviceListCriteria criteria);
}
