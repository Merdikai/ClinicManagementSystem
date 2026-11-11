using ClinicManagementSystem.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, 
        Exception ex, 
        CancellationToken ct)
    {
        logger.LogError(ex, "Unhandled exception: {Message}. TraceIdentifier: {TraceId}", ex.Message, ctx.TraceIdentifier);

        var (statusCode, title, detail) = ex switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "One or more validation errors occurred."
            ),
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFound.Message
            ),
            SecurityAlertException secAlert => (
                StatusCodes.Status401Unauthorized,
                "Security Alert",
                secAlert.Message
            ),
            ForbiddenAccessException forbidden => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbidden.Message
            ),
            UnauthorizedAccessException unauthorized => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                unauthorized.Message
            ),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message
            ),
            BusinessRuleViolationException bizEx => (
                StatusCodes.Status409Conflict,
                "Business Rule Violation",
                bizEx.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server Error",
                $"An unexpected error occurred. ID: {ctx.TraceIdentifier}"
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path
        };

        problemDetails.Extensions["traceId"] = ctx.TraceIdentifier;

        if (ex is ValidationException vEx)
        {
            var errors = vEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            problemDetails.Extensions["errors"] = errors;
        }

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }
}

