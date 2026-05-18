using System.Reflection;

namespace TechInventory.Application.Common.Results;

public static class ResultFactory
{
    public static TResponse CreateFailure<TResponse>(Error error)
        where TResponse : Result
    {
        ArgumentNullException.ThrowIfNull(error);

        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod(
                nameof(Result<object>.Failure),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Error)],
                modifiers: null);

            if (failureMethod is not null)
            {
                return (TResponse)failureMethod.Invoke(null, [error])!;
            }
        }

        throw new InvalidOperationException($"Unsupported result type '{typeof(TResponse).FullName}'.");
    }
}
