using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.ExceptionHandling;
using TechInventory.Application.Common.Results;

namespace TechInventory.Api.Common;

public static class ControllerResultExtensions
{
    public static ActionResult<T> OkResult<T>(this ControllerBase controller, Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return controller.Ok(result.GetValueOrThrow());
    }

    public static ActionResult<T> CreatedAtActionResult<T>(
        this ControllerBase controller,
        string actionName,
        Result<T> result,
        Func<T, object> routeValuesFactory)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(routeValuesFactory);

        var value = result.GetValueOrThrow();
        return controller.CreatedAtAction(actionName, routeValuesFactory(value), value);
    }

    public static IActionResult NoContentResult(this ControllerBase controller, Result result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        result.EnsureSuccessOrThrow();
        return controller.NoContent();
    }

    public static IActionResult NoContentResult<T>(this ControllerBase controller, Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        result.EnsureSuccessOrThrow();
        return controller.NoContent();
    }

    public static T GetValueOrThrow<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            throw new ResultFailureException(result.Error!);
        }

        return result.Value!;
    }

    public static void EnsureSuccessOrThrow(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsFailure)
        {
            throw new ResultFailureException(result.Error!);
        }
    }
}
