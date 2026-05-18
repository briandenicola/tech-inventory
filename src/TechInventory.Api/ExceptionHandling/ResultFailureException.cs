using TechInventory.Application.Common.Results;

namespace TechInventory.Api.ExceptionHandling;

public sealed class ResultFailureException(Error error) : Exception(error?.Message)
{
    public Error Error { get; } = error ?? throw new ArgumentNullException(nameof(error));
}
