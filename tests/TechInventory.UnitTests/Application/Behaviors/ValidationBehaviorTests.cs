using FluentAssertions;
using FluentValidation;
using MediatR;
using TechInventory.Application.Behaviors;
using TechInventory.Application.Common.Results;

namespace TechInventory.UnitTests.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsFailureResultWithValidationDictionary()
    {
        var behavior = new ValidationBehavior<PublicSampleCommand, Result<string>>([new PublicSampleCommandValidator()]);

        var result = await behavior.Handle(
            new PublicSampleCommand(string.Empty),
            _ => Task.FromResult(Result<string>.Success("unexpected")),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Validation");
        result.Error.Message.Should().Be("One or more validation failures occurred.");
        result.Error.ValidationErrors.Should().ContainKey(nameof(PublicSampleCommand.Name));
        result.Error.ValidationErrors[nameof(PublicSampleCommand.Name)].Should().Contain("Name is required.");
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_InvokesTheInnerHandler()
    {
        var behavior = new ValidationBehavior<PublicSampleCommand, Result<string>>([new PublicSampleCommandValidator()]);
        var expected = Result<string>.Success("accepted");
        var nextCallCount = 0;

        var result = await behavior.Handle(
            new PublicSampleCommand("Nostromo"),
            _ =>
            {
                nextCallCount++;
                return Task.FromResult(expected);
            },
            CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        nextCallCount.Should().Be(1);
    }

    public sealed record PublicSampleCommand(string Name) : IRequest<Result<string>>;

    public sealed class PublicSampleCommandValidator : AbstractValidator<PublicSampleCommand>
    {
        public PublicSampleCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .WithMessage("Name is required.");
        }
    }
}
