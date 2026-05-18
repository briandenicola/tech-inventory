using System.Reflection;
using FluentAssertions;
using TechInventory.Domain.Enums;
using TechInventory.UnitTests.Support;

namespace TechInventory.UnitTests.Domain;

public class AuditEventTests
{
    [Fact]
    public void AuditEvent_ConstructionCapturesActorActionTimestampBeforeAndAfterCorrectly()
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");
        var auditEvent = ContractReflectionAssertions.CreateInstance(
            ContractReflectionAssertions.RequirePrimaryConstructor(auditEventType),
            ContractReflectionAssertions.AuditSamples,
            ("actorId", ContractReflectionAssertions.AuditSamples.Actor),
            ("beforePayload", ContractReflectionAssertions.AuditSamples.BeforeJson),
            ("afterPayload", ContractReflectionAssertions.AuditSamples.AfterJson),
            ("action", AuditAction.Updated));

        var actorProperty = ContractReflectionAssertions.RequireProperty(auditEventType, "ActorId", "Actor", "UserId", "User");
        var actionProperty = ContractReflectionAssertions.RequireProperty(auditEventType, "Action");
        var timestampProperty = ContractReflectionAssertions.RequireProperty(auditEventType, "Timestamp", "OccurredAt", "OccurredOn");
        var beforeProperty = ContractReflectionAssertions.RequireProperty(auditEventType, "BeforePayload", "Before");
        var afterProperty = ContractReflectionAssertions.RequireProperty(auditEventType, "AfterPayload", "After");

        actorProperty.GetValue(auditEvent).Should().Be(ContractReflectionAssertions.AuditSamples.Actor);
        actionProperty.GetValue(auditEvent).Should().Be(AuditAction.Updated);
        timestampProperty.GetValue(auditEvent).Should().Be(ContractReflectionAssertions.AuditSamples.Timestamp);
        beforeProperty.GetValue(auditEvent).Should().Be(ContractReflectionAssertions.AuditSamples.BeforeJson);
        afterProperty.GetValue(auditEvent).Should().Be(ContractReflectionAssertions.AuditSamples.AfterJson);
    }

    [Fact]
    public void AuditEvent_TimestampsAreUtcAndMonotonic()
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");
        var timestampProperty = ContractReflectionAssertions.RequireProperty(auditEventType, "Timestamp", "OccurredAt", "OccurredOn");
        var autoTimestampConstructor = auditEventType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(constructor => constructor.GetParameters().All(parameter => !string.Equals(ContractReflectionAssertions.Normalize(parameter.Name), "TIMESTAMP", StringComparison.Ordinal)));

        var beforeConstruction = DateTimeOffset.UtcNow;
        var auditEvent = autoTimestampConstructor is null
            ? ContractReflectionAssertions.CreateAuditEvent(auditEventType)
            : ContractReflectionAssertions.CreateInstance(
                autoTimestampConstructor,
                ContractReflectionAssertions.AuditSamples,
                ("beforePayload", ContractReflectionAssertions.AuditSamples.BeforeJson),
                ("afterPayload", ContractReflectionAssertions.AuditSamples.AfterJson),
                ("action", AuditAction.Updated));
        var afterConstruction = DateTimeOffset.UtcNow;

        var timestamp = timestampProperty.GetValue(auditEvent);

        timestamp.Should().NotBeNull();
        ContractReflectionAssertions.IsUtc(timestamp!).Should().BeTrue();
        ContractReflectionAssertions.ToDateTimeOffset(timestamp!).Should().BeOnOrAfter(beforeConstruction);
        ContractReflectionAssertions.ToDateTimeOffset(timestamp!).Should().BeOnOrBefore(afterConstruction);
    }

    [Fact]
    public void AuditEvent_ExposesNoPublicSettersOrMutationMethods()
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");

        auditEventType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Should()
            .OnlyContain(property => property.SetMethod == null || !property.SetMethod.IsPublic);

        var publicMutationMethods = auditEventType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => !method.IsSpecialName && method.DeclaringType != typeof(object))
            .Select(method => method.Name)
            .ToArray();

        publicMutationMethods.Should().BeEmpty();
    }

    [Theory]
    [InlineData("EntityType", "ENTITYTYPE")]
    [InlineData("EntityId", "ENTITYID")]
    public void AuditEvent_RejectsNullOrEmptyRequiredStringFields(string parameterLabel, params string[] candidateNames)
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");
        var constructor = ContractReflectionAssertions.RequirePrimaryConstructor(auditEventType);
        var parameter = ContractReflectionAssertions.RequireStringParameter(constructor, candidateNames);

        Action constructWithNull = () => constructor.Invoke(ContractReflectionAssertions.CreateConstructorArgumentsWithOverride(constructor, parameter.Name!, null));
        Action constructWithEmpty = () => constructor.Invoke(ContractReflectionAssertions.CreateConstructorArgumentsWithOverride(constructor, parameter.Name!, string.Empty));

        constructWithNull.Should().Throw<Exception>()
            .Where(exception => ContractReflectionAssertions.Unwrap(exception) is ArgumentException || ContractReflectionAssertions.Unwrap(exception) is ArgumentNullException,
                $"{parameterLabel} should reject null construction input.");

        constructWithEmpty.Should().Throw<Exception>()
            .Where(exception => ContractReflectionAssertions.Unwrap(exception) is ArgumentException || ContractReflectionAssertions.Unwrap(exception) is ArgumentNullException,
                $"{parameterLabel} should reject empty construction input.");
    }

    [Fact]
    public void AuditEvent_CreatedEventsRequireAnAfterPayload()
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");
        var constructor = ContractReflectionAssertions.RequirePrimaryConstructor(auditEventType);

        Action act = () => ContractReflectionAssertions.CreateInstance(
            constructor,
            ContractReflectionAssertions.AuditSamples,
            ("action", AuditAction.Created),
            ("beforePayload", null),
            ("afterPayload", null));

        act.Should().Throw<Exception>()
            .Where(exception => ContractReflectionAssertions.Unwrap(exception) is ArgumentException,
                "created audit events must carry an after payload.");
    }

    [Fact]
    public void AuditEvent_RejectsTheDefaultTimestamp()
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");
        var constructor = ContractReflectionAssertions.RequirePrimaryConstructor(auditEventType);

        Action act = () => ContractReflectionAssertions.CreateInstance(
            constructor,
            ContractReflectionAssertions.AuditSamples,
            ("action", AuditAction.Created),
            ("timestamp", default(DateTimeOffset)),
            ("beforePayload", "null"),
            ("afterPayload", ContractReflectionAssertions.AuditSamples.AfterJson));

        act.Should().Throw<Exception>()
            .Where(exception => ContractReflectionAssertions.Unwrap(exception) is ArgumentOutOfRangeException,
                "audit events must reject missing timestamps.");
    }

    [Fact]
    public void AuditEvent_UpdatedEventsRequireBeforeAndAfterPayloads()
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");
        var constructor = ContractReflectionAssertions.RequirePrimaryConstructor(auditEventType);

        Action missingBefore = () => ContractReflectionAssertions.CreateInstance(
            constructor,
            ContractReflectionAssertions.AuditSamples,
            ("action", AuditAction.Updated),
            ("beforePayload", null),
            ("afterPayload", ContractReflectionAssertions.AuditSamples.AfterJson));
        Action missingAfter = () => ContractReflectionAssertions.CreateInstance(
            constructor,
            ContractReflectionAssertions.AuditSamples,
            ("action", AuditAction.Updated),
            ("beforePayload", ContractReflectionAssertions.AuditSamples.BeforeJson),
            ("afterPayload", null));

        missingBefore.Should().Throw<Exception>()
            .Where(exception => ContractReflectionAssertions.Unwrap(exception) is ArgumentException,
                "updated audit events must carry a before payload.");
        missingAfter.Should().Throw<Exception>()
            .Where(exception => ContractReflectionAssertions.Unwrap(exception) is ArgumentException,
                "updated audit events must carry an after payload.");
    }

    [Fact]
    public void AuditEvent_DeletedEventsRequireABeforePayload()
    {
        var auditEventType = ContractReflectionAssertions.RequireDomainType("TechInventory.Domain.Entities.AuditEvent", "awaiting Hicks T11");
        var constructor = ContractReflectionAssertions.RequirePrimaryConstructor(auditEventType);

        Action act = () => ContractReflectionAssertions.CreateInstance(
            constructor,
            ContractReflectionAssertions.AuditSamples,
            ("action", AuditAction.Deleted),
            ("beforePayload", null),
            ("afterPayload", null));

        act.Should().Throw<Exception>()
            .Where(exception => ContractReflectionAssertions.Unwrap(exception) is ArgumentException,
                "deleted audit events must carry a before payload.");
    }
}
