using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Devices;
using TechInventory.Application.Devices.Commands;
using TechInventory.Application.Tags;
using TechInventory.Application.Tags.Commands;
using TechInventory.Application.Tags.Queries;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T27TagHandlerScaffoldingTests
{
    [Fact]
    public async Task CreateTagCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var deps = CreateTagDependencies();
        deps.TagRepository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Tag>.Failure(Error.NotFound("missing")));
        deps.TagRepository.AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Tag>.Success(call.Arg<Tag>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new CreateTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateTagCommand("Network", "#336699"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Network");
        result.Value.Color.Should().Be("#336699");
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Tag) && entry.Action == AuditAction.Created));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTagCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new CreateTagCommand(string.Empty),
            new CreateTagCommandValidator(),
            TagResponse.FromEntity(DeviceHandlerTestSupport.CreateTag()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task CreateTagCommandHandler_WhenDuplicateNameExists_ReturnsConflictFailure()
    {
        var deps = CreateTagDependencies();
        deps.TagRepository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Tag>.Success(DeviceHandlerTestSupport.CreateTag()));
        var handler = new CreateTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateTagCommand("Network"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task UpdateTagCommandHandler_WhenValidInput_ReturnsSuccessAndCapturesBeforeSnapshot()
    {
        var deps = CreateTagDependencies();
        var tag = new Tag(Guid.NewGuid(), "Network");
        deps.TagRepository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        deps.TagRepository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Tag>.Failure(Error.NotFound("missing")));
        deps.TagRepository.UpdateAsync(tag, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new UpdateTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateTagCommand(tag.Id, "Wireless", "#aabbcc"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Wireless");
        tag.Name.Should().Be("Wireless");
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Tag) &&
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Network", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTagCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new UpdateTagCommand(Guid.Empty, string.Empty),
            new UpdateTagCommandValidator(),
            TagResponse.FromEntity(DeviceHandlerTestSupport.CreateTag()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task UpdateTagCommandHandler_WhenTagDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateTagDependencies();
        var id = Guid.NewGuid();
        deps.TagRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Tag>.Failure(Error.NotFound("missing")));
        var handler = new UpdateTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateTagCommand(id, "Wireless"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateTagCommandHandler_WhenDuplicateNameBelongsToDifferentTag_ReturnsConflictFailure()
    {
        var deps = CreateTagDependencies();
        var tag = new Tag(Guid.NewGuid(), "Network");
        var other = new Tag(Guid.NewGuid(), "Wireless");
        deps.TagRepository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        deps.TagRepository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Tag>.Success(other));
        var handler = new UpdateTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateTagCommand(tag.Id, "Wireless"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteTagCommandHandler_WhenTagExists_DeactivatesAndCapturesBeforeSnapshot()
    {
        var deps = CreateTagDependencies();
        var tag = new Tag(Guid.NewGuid(), "Network");
        deps.TagRepository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        deps.TagRepository.UpdateAsync(tag, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new DeleteTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteTagCommand(tag.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tag.IsActive.Should().BeFalse();
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Tag) &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Network", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTagCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new DeleteTagCommand(Guid.Empty), new DeleteTagCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task DeleteTagCommandHandler_WhenTagDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateTagDependencies();
        var id = Guid.NewGuid();
        deps.TagRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Tag>.Failure(Error.NotFound("missing")));
        var handler = new DeleteTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteTagCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteTagCommandHandler_WhenTagAlreadyInactive_ReturnsConflictFailure()
    {
        var deps = CreateTagDependencies();
        var tag = DeviceHandlerTestSupport.CreateTag(isActive: false);
        deps.TagRepository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        var handler = new DeleteTagCommandHandler(deps.TagRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteTagCommand(tag.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task GetTagByIdQueryHandler_WhenTagExists_ReturnsSuccess()
    {
        var deps = CreateTagDependencies();
        var tag = DeviceHandlerTestSupport.CreateTag();
        deps.TagRepository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        var handler = new GetTagByIdQueryHandler(deps.TagRepository);

        var result = await handler.Handle(new GetTagByIdQuery(tag.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(tag.Id);
        result.Value.Name.Should().Be(tag.Name);
    }

    [Fact]
    public async Task GetTagByIdQueryHandler_WhenTagDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateTagDependencies();
        var id = Guid.NewGuid();
        deps.TagRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Tag>.Failure(Error.NotFound("missing")));
        var handler = new GetTagByIdQueryHandler(deps.TagRepository);

        var result = await handler.Handle(new GetTagByIdQuery(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ListTagsQueryHandler_WhenRequested_ReturnsAllItems()
    {
        var deps = CreateTagDependencies();
        var tags = new[]
        {
            new Tag(Guid.NewGuid(), "Network"),
            new Tag(Guid.NewGuid(), "Wireless"),
        };
        deps.TagRepository.ListAsync(false, Arg.Any<CancellationToken>()).Returns(tags);
        var handler = new ListTagsQueryHandler(deps.TagRepository);

        var result = await handler.Handle(new ListTagsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Select(t => t.Name).Should().Contain(new[] { "Network", "Wireless" });
    }

    [Fact]
    public async Task AddTagToDeviceCommandHandler_WhenValidInput_AddsTheJoinEntity()
    {
        var dependencies = CreateDependencies();
        var device = DeviceHandlerTestSupport.CreateDevice();
        var tag = DeviceHandlerTestSupport.CreateTag();
        var deviceTag = new DeviceTag(device.Id, tag.Id);
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.TagRepository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        dependencies.DeviceRepository.UpsertTagAsync(Arg.Any<DeviceTag>(), Arg.Any<CancellationToken>()).Returns(Result<DeviceTag>.Success(deviceTag));
        dependencies.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new AddTagToDeviceCommandHandler(dependencies.DeviceRepository, dependencies.TagRepository, dependencies.UnitOfWork, dependencies.AuditContext);

        var result = await handler.Handle(new AddTagToDeviceCommand(device.Id, tag.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeviceId.Should().Be(device.Id);
        result.Value.TagId.Should().Be(tag.Id);
        dependencies.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == "DeviceTag" &&
            entry.Action == AuditAction.Created));
        await dependencies.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddTagToDeviceCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new AddTagToDeviceCommand(Guid.Empty, Guid.Empty), new AddTagToDeviceCommandValidator(), DeviceHandlerTestSupport.SampleDeviceTagResponse());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task AddTagToDeviceCommandHandler_WhenRepositoryDetectsAConflict_ReturnsConflictFailure()
    {
        var dependencies = CreateDependencies();
        var device = DeviceHandlerTestSupport.CreateDevice();
        var tag = DeviceHandlerTestSupport.CreateTag();
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.TagRepository.GetByIdAsync(tag.Id, Arg.Any<CancellationToken>()).Returns(Result<Tag>.Success(tag));
        dependencies.DeviceRepository.UpsertTagAsync(Arg.Any<DeviceTag>(), Arg.Any<CancellationToken>()).Returns(Result<DeviceTag>.Failure(Error.Conflict("Tag already assigned.")));
        var handler = new AddTagToDeviceCommandHandler(dependencies.DeviceRepository, dependencies.TagRepository, dependencies.UnitOfWork, dependencies.AuditContext);

        var result = await handler.Handle(new AddTagToDeviceCommand(device.Id, tag.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task RemoveTagFromDeviceCommandHandler_WhenValidInput_RemovesTheJoinEntity()
    {
        var dependencies = CreateDependencies();
        var device = DeviceHandlerTestSupport.CreateDevice();
        var existingTag = new DeviceTag(device.Id, Guid.NewGuid());
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.DeviceRepository.ListTagsAsync(device.Id, Arg.Any<CancellationToken>()).Returns([existingTag]);
        dependencies.DeviceRepository.RemoveTagAsync(device.Id, existingTag.TagId, Arg.Any<CancellationToken>()).Returns(Result.Success());
        dependencies.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new RemoveTagFromDeviceCommandHandler(dependencies.DeviceRepository, dependencies.UnitOfWork, dependencies.AuditContext);

        var result = await handler.Handle(new RemoveTagFromDeviceCommand(device.Id, existingTag.TagId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dependencies.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == "DeviceTag" &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains(existingTag.TagId.ToString(), StringComparison.Ordinal)));
        await dependencies.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveTagFromDeviceCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new RemoveTagFromDeviceCommand(Guid.Empty, Guid.Empty), new RemoveTagFromDeviceCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task RemoveTagFromDeviceCommandHandler_WhenJoinEntityDoesNotExist_ReturnsNotFoundFailure()
    {
        var dependencies = CreateDependencies();
        var device = DeviceHandlerTestSupport.CreateDevice();
        var tagId = Guid.NewGuid();
        dependencies.DeviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        dependencies.DeviceRepository.ListTagsAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<DeviceTag>());
        var handler = new RemoveTagFromDeviceCommandHandler(dependencies.DeviceRepository, dependencies.UnitOfWork, dependencies.AuditContext);

        var result = await handler.Handle(new RemoveTagFromDeviceCommand(device.Id, tagId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    private static TagCommandDependencies CreateDependencies() => new(
        Substitute.For<IDeviceRepository>(),
        Substitute.For<ITagRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private static TagOnlyDependencies CreateTagDependencies() => new(
        Substitute.For<ITagRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record TagCommandDependencies(
        IDeviceRepository DeviceRepository,
        ITagRepository TagRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);

    private sealed record TagOnlyDependencies(
        ITagRepository TagRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);
}
