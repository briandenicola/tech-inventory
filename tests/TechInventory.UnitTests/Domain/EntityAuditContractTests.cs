using FluentAssertions;
using TechInventory.Domain.Primitives;

namespace TechInventory.UnitTests.Domain;

public class EntityAuditContractTests
{
    [Fact]
    public void Entity_RejectsAnEmptyId()
    {
        var act = () => new TestEntity(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Entity_SetAuditMetadata_AssignsAndTrimsAuditFields()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var modifiedAt = createdAt.AddMinutes(5);

        entity.SetAuditMetadata(createdAt, modifiedAt, "  ripley  ", "  apone  ");

        entity.CreatedAt.Should().Be(createdAt);
        entity.ModifiedAt.Should().Be(modifiedAt);
        entity.CreatedBy.Should().Be("ripley");
        entity.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Entity_SetAuditMetadata_RejectsModifiedAtEarlierThanCreatedAt()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var modifiedAt = createdAt.AddMinutes(-1);
        var act = () => entity.SetAuditMetadata(createdAt, modifiedAt);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Entity_Touch_UpdatesModifiedByAndAdvancesModifiedAt()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var beforeTouch = entity.ModifiedAt;

        entity.ApplyTouch("  hicks  ");

        entity.ModifiedAt.Should().BeOnOrAfter(beforeTouch);
        entity.ModifiedBy.Should().Be("hicks");
    }

    [Fact]
    public void AggregateRoot_InheritsEntityIdentity()
    {
        var id = Guid.NewGuid();
        var aggregate = new TestAggregate(id);

        aggregate.Id.Should().Be(id);
    }

    private sealed class TestEntity(Guid id) : Entity(id)
    {
        public void ApplyTouch(string? modifiedBy = null)
        {
            Touch(modifiedBy);
        }
    }

    private sealed class TestAggregate(Guid id) : AggregateRoot(id);
}
