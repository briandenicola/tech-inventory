using TechInventory.Domain.Primitives;

namespace TechInventory.Domain.Entities;

public sealed class DeviceTag(Guid deviceId, Guid tagId)
{
    public Guid DeviceId { get; } = Guard.AgainstDefault(deviceId, nameof(deviceId));

    public Guid TagId { get; } = Guard.AgainstDefault(tagId, nameof(tagId));

    public bool IsActive { get; private set; } = true;

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }
}
