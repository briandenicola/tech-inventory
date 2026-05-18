using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Domain;

public class AuditEventContractTests
{
    [Fact]
    public void AuditEvent_RequiresEntityMetadata()
    {
        var act = () => new AuditEvent(Guid.NewGuid(), string.Empty, string.Empty, string.Empty, AuditAction.Created, DateTimeOffset.UtcNow, string.Empty, string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuditEvent_CapturesActorActionAndPayloadsAtCreation()
    {
        var timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            "  apone  ",
            "Device",
            "device-123",
            AuditAction.Updated,
            timestamp,
            "  {\"name\":\"Old\"}  ",
            "  {\"name\":\"New\"}  ");

        auditEvent.Actor.Should().Be("apone");
        auditEvent.EntityType.Should().Be("Device");
        auditEvent.EntityId.Should().Be("device-123");
        auditEvent.Action.Should().Be(AuditAction.Updated);
        auditEvent.Timestamp.Should().Be(timestamp);
        auditEvent.BeforePayload.Should().Be("{\"name\":\"Old\"}");
        auditEvent.AfterPayload.Should().Be("{\"name\":\"New\"}");
    }

    [Fact]
    public void AuditEvent_RequiresBeforeAndAfterPayloads()
    {
        var act = () => new AuditEvent(Guid.NewGuid(), "apone", "Device", "device-123", AuditAction.Updated, DateTimeOffset.UtcNow, string.Empty, string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuditEvent_ExposesNoWritablePublicPropertiesOrMutators()
    {
        var publicSetters = typeof(AuditEvent)
            .GetProperties()
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToArray();

        var publicDeclaredMethods = typeof(AuditEvent)
            .GetMethods()
            .Where(method => method.IsPublic && !method.IsSpecialName && method.DeclaringType == typeof(AuditEvent))
            .Select(method => method.Name)
            .ToArray();

        publicSetters.Should().BeEmpty();
        publicDeclaredMethods.Should().BeEmpty();
    }
}
