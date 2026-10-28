using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RondiTrack.Exceptions;
using System.Diagnostics;
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

    // Handle application exceptions and return ProblemDetails.
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Determine the appropriate HTTP status code.
        var statusCode = exception switch
        {
            RequestValidationException =>
                StatusCodes.Status400BadRequest,

            NotFoundException =>
                StatusCodes.Status404NotFound,

            BusinessRuleException =>
                StatusCodes.Status422UnprocessableEntity,

            DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: "23505"
                }
            } =>
                StatusCodes.Status422UnprocessableEntity,

            IdempotencyConflictException =>
                StatusCodes.Status409Conflict,

            DuplicateContributionException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        // Use the W3C trace ID for request correlation.
        var correlationId =
            Activity.Current?.TraceId.ToString()
            ?? httpContext.TraceIdentifier;

        // Log expected client errors at Information level.
        // Do not log exception messages or request data.
        if (statusCode < 500)
        {
            _logger.LogInformation(
                "Request failed. ExceptionType={ExceptionType}, StatusCode={StatusCode}, CorrelationId={CorrelationId}",
                exception.GetType().Name,
                statusCode,
                correlationId);
        }
        else
        {
            // Unexpected server errors require full exception details.
            _logger.LogError(
                exception,
                "Unexpected server error. CorrelationId={CorrelationId}",
                correlationId);
        }

        // Create a safe ProblemDetails response.
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

                DbUpdateException
                {
                    InnerException: PostgresException
                    {
                        SqlState: "23505"
                    }
                } =>
                    "Duplicate resource",

                IdempotencyConflictException =>
                    "Idempotency conflict",

                DuplicateContributionException =>
                    "Duplicate contribution",

                _ =>
                    "Internal server error"
            },

            // Use fixed messages to avoid leaking sensitive data.
            Detail = statusCode switch
            {
                400 => "The request is invalid.",
                404 => "The requested resource was not found.",
                409 => "The request conflicts with an existing operation.",
                422 => "The request could not be processed.",
                _ => "An unexpected error occurred."
            }
        };

        // Include the same W3C trace ID in the response.
        problemDetails.Extensions["correlationId"] =
            correlationId;

        // Return application/problem+json.
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType =
            "application/problem+json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails),
            cancellationToken);

        return true;
    }
}