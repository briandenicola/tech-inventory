using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TechInventory.Application.Common.Results;

namespace TechInventory.Api.ExceptionHandling;

public sealed class ApiExceptionHandler(
    ProblemDetailsFactory problemDetailsFactory,
    IHostEnvironment hostEnvironment,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        ProblemDetails problemDetails;
        if (exception is ResultFailureException resultFailureException)
        {
            problemDetails = CreateProblemDetails(httpContext, resultFailureException.Error);
        }
        else
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            problemDetails = problemDetailsFactory.CreateProblemDetails(
                httpContext,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: hostEnvironment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                instance: httpContext.Request.Path);

            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync((object)problemDetails, cancellationToken);
        return true;
    }

    private ProblemDetails CreateProblemDetails(HttpContext httpContext, Error error)
    {
        var statusCode = MapStatusCode(error);
        if (string.Equals(error.Code, "Validation", StringComparison.OrdinalIgnoreCase))
        {
            var modelState = new ModelStateDictionary();
            foreach (var validationError in error.ValidationErrors)
            {
                foreach (var message in validationError.Value)
                {
                    modelState.AddModelError(validationError.Key, message);
                }
            }

            var validationProblemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                httpContext,
                modelState,
                statusCode: statusCode,
                title: "Bad Request",
                detail: error.Message,
                instance: httpContext.Request.Path);

            validationProblemDetails.Extensions["code"] = error.Code;
            validationProblemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
            return validationProblemDetails;
        }

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            httpContext,
            statusCode: statusCode,
            title: MapTitle(statusCode),
            detail: error.Message,
            instance: httpContext.Request.Path);

        problemDetails.Extensions["code"] = error.Code;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problemDetails;
    }

    private static int MapStatusCode(Error error)
        => error.Code switch
        {
            "Validation" => StatusCodes.Status400BadRequest,
            "NotFound" => StatusCodes.Status404NotFound,
            "Conflict" => StatusCodes.Status409Conflict,
            "PayloadTooLarge" => StatusCodes.Status413PayloadTooLarge,
            _ => StatusCodes.Status400BadRequest,
        };

    private static string MapTitle(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status413PayloadTooLarge => "Payload Too Large",
            _ => "Bad Request",
        };
}
