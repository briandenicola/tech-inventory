using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Devices.Feed;

namespace TechInventory.UnitTests.Application;

public sealed class DeviceFeedQueryTests
{
    private static DeviceFeedItem Item(string name) => new(
        Guid.NewGuid(), name, null, null, "Computers", "Brian", "Office", null, [], "Active",
        null, null, "USD", null, null, null, null, null, null,
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

    private static IDeviceFeedReader Reader(int totalCount, params DeviceFeedItem[] items)
    {
        var reader = Substitute.For<IDeviceFeedReader>();
        reader.ReadPageAsync(Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new PagedResult<DeviceFeedItem>(items, totalCount, call.Arg<PageRequest>().Page, call.Arg<PageRequest>().PageSize));
        return reader;
    }

    [Fact]
    public async Task Handle_WrapsEachDeviceAsARecordAndOffersTheNextPage()
    {
        var laptop = Item("Laptop");
        var reader = Reader(3, laptop, Item("Router"));

        var result = await new ListDeviceFeedQueryHandler(reader).Handle(new ListDeviceFeedQuery(null, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Schema.Should().Be(DeviceFeedResponse.SchemaName);
        result.Value.Next.Should().Be("2");
        var record = result.Value.Records[0];
        record.Kind.Should().Be("device");
        record.Id.Should().Be(laptop.Id.ToString());
        record.UpdatedAt.Should().Be(laptop.ModifiedAt);
        record.Data.Should().BeSameAs(laptop);
        await reader.Received(1).ReadPageAsync(Arg.Is<PageRequest>(p => p.Page == 1 && p.PageSize == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnTheLastPageOffersNoNext()
    {
        var reader = Reader(3, Item("Old phone"));

        var result = await new ListDeviceFeedQueryHandler(reader).Handle(new ListDeviceFeedQuery("2", 2), CancellationToken.None);

        result.Value!.Next.Should().BeNull();
        await reader.Received(1).ReadPageAsync(Arg.Is<PageRequest>(p => p.Page == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoDevicesOffersNoNext()
    {
        var result = await new ListDeviceFeedQueryHandler(Reader(0)).Handle(new ListDeviceFeedQuery(), CancellationToken.None);

        result.Value!.Records.Should().BeEmpty();
        result.Value.Next.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 25, true)]
    [InlineData("", 25, true)]
    [InlineData("3", 200, true)]
    [InlineData("abc", 25, false)]
    [InlineData("0", 25, false)]
    [InlineData("-1", 25, false)]
    [InlineData(" 2", 25, false)]
    [InlineData("1", 0, false)]
    [InlineData("1", 201, false)]
    public void Validator_AcceptsOnlyTokensItOffersAndBoundedLimits(string? page, int limit, bool valid)
    {
        var result = new ListDeviceFeedQueryValidator().Validate(new ListDeviceFeedQuery(page, limit));

        result.IsValid.Should().Be(valid);
    }
}
