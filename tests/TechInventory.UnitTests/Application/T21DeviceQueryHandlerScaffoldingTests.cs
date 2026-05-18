using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Devices;
using TechInventory.Application.Devices.Queries;
using TechInventory.Domain.Entities;

namespace TechInventory.UnitTests.Application;

public sealed class T21DeviceQueryHandlerScaffoldingTests
{
    [Fact]
    public async Task GetDeviceByIdQueryHandler_WhenDeviceExists_ReturnsSuccess()
    {
        var repository = Substitute.For<IDeviceRepository>();
        var device = DeviceHandlerTestSupport.CreateDevice();
        repository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        var handler = new GetDeviceByIdQueryHandler(repository);

        var result = await handler.Handle(new GetDeviceByIdQuery(device.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(device.Id);
        result.Value.Name.Should().Be(device.Name);
    }

    [Fact]
    public async Task GetDeviceByIdQueryHandler_WhenDeviceDoesNotExist_ReturnsNotFoundFailure()
    {
        var repository = Substitute.For<IDeviceRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<Device>.Failure(Error.NotFound("Device missing.")));
        var handler = new GetDeviceByIdQueryHandler(repository);

        var result = await handler.Handle(new GetDeviceByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ListDevicesQueryHandler_WhenFiltersAreValid_ReturnsPagedResponseShape()
    {
        var repository = Substitute.For<IDeviceRepository>();
        var firstDevice = DeviceHandlerTestSupport.CreateDevice();
        var secondDevice = DeviceHandlerTestSupport.CreateDevice();
        repository.ListAsync(Arg.Any<DeviceListCriteria>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Device>([firstDevice, secondDevice], totalCount: 4, page: 2, pageSize: 2));
        var handler = new ListDevicesQueryHandler(repository);

        var result = await handler.Handle(new ListDevicesQuery(Page: 2, PageSize: 2, SortBy: "name"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(4);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListDevicesQueryHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var query = new ListDevicesQuery(Page: 0, PageSize: 500, SortBy: "bogus");

        var result = await DeviceHandlerTestSupport.ValidateAsync(query, new ListDevicesQueryValidator(), new PagedResponse<DeviceResponse>([], 0, 1, 25));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }
}
