using TechInventory.Domain.Entities;

namespace TechInventory.Application.Devices;

public sealed record DeviceTagResponse(Guid DeviceId, Guid TagId, bool IsActive)
{
    public static DeviceTagResponse FromEntity(DeviceTag deviceTag)
    {
        ArgumentNullException.ThrowIfNull(deviceTag);
        return new DeviceTagResponse(deviceTag.DeviceId, deviceTag.TagId, deviceTag.IsActive);
    }
}
