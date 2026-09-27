using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RondiTrack.Exceptions;
using System.Text.Json;

namespace RondiTrack.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    // Handle exceptions and return the appropriate HTTP response
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Decide which HTTP status code matches the exception
        var statusCode = exception switch
        {
            RequestValidationException =>
                StatusCodes.Status400BadRequest,

            NotFoundException =>
                StatusCodes.Status404NotFound,

            BusinessRuleException =>
                StatusCodes.Status422UnprocessableEntity,

            IdempotencyConflictException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        // Create the standard ProblemDetails response
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,

            Title = exception switch
            {
                RequestValidationException =>
                    "Validation failed",

                NotFoundException =>
                    "Resource not found",

                BusinessRuleException =>
                    "Business rule violated",

                IdempotencyConflictException =>
                    "Idempotency conflict",

                _ =>
                    "Internal server error"
            },

            // Do not expose internal exception details for unexpected errors
            Detail = statusCode ==
                     StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message
        };

        // Get the correlation ID
        var correlationId = httpContext.TraceIdentifier;

        // Add the correlation ID to the response
        problemDetails.Extensions["correlationId"] =
            correlationId;

        // Log the exception with the same correlation ID
        _logger.LogError(
            exception,
            "Request failed with correlation ID {CorrelationId}",
            correlationId);

        // Return ProblemDetails as problem+json
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails);

        await httpContext.Response.WriteAsync(
            json,
            cancellationToken);

        return true;
    }
}