using FluentValidation;
using MediatR;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private const string RequestErrorKey = "request";
    private readonly IValidator<TRequest>[] _validators = (validators ?? throw new ArgumentNullException(nameof(validators))).ToArray();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task
            .WhenAll(_validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var validationErrors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .GroupBy(
                failure => string.IsNullOrWhiteSpace(failure.PropertyName) ? RequestErrorKey : failure.PropertyName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(failure => failure.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Select(message => message.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        if (validationErrors.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var error = new Error("Validation", "One or more validation failures occurred.", validationErrors);
        return ResultFactory.CreateFailure<TResponse>(error);
    }
}
