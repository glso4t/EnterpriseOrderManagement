using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseOrderManagement.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isConcurrencyConflict = exception is DbUpdateConcurrencyException;
        var statusCode = StatusCodes.Status500InternalServerError;
        if (isConcurrencyConflict)
        {
            statusCode = StatusCodes.Status409Conflict;
            logger.LogWarning(
                exception,
                "Concurrency conflict while processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        httpContext.Response.StatusCode =statusCode;
        var title = "An unexpected error occurred.";
        var detail = "The server encountered an unexpected error. Please try again later.";
        if (isConcurrencyConflict)
        {
            title = "Concurrency conflict.";
            detail = "The data changed while your request was being processed. Reload the latest data and try again.";
        }
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path.ToString()
        };
        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
        return true;
    }
}