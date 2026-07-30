using ClinicManagementSystem.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ClinicManagementSystem.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Not found: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteProblemDetails(context, "Not Found", ex.Message, StatusCodes.Status404NotFound);
        }
        catch (BusinessRuleViolationException ex)
        {
            _logger.LogWarning("Business rule violation: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await WriteProblemDetails(context, "Business Rule Violation", ex.Message, StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteProblemDetails(context, "Internal Server Error", "An unexpected error occurred.", StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, string title, string detail, int statusCode)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}