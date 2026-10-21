using RondiTrack.Repositories;
using Scalar.AspNetCore;
using RondiTrack.Services;
using RondiTrack.Idempotency;
using FluentValidation;
using FluentValidation.AspNetCore;
using RondiTrack.Validators;
using RondiTrack.ExceptionHandling;
using RondiTrack.Authorization;
using RondiTrack.Data;
using RondiTrack.HealthChecks;
using RondiTrack.Filters;
using System.Text.Json;
// Import the background service responsible for monthly cycle closing.
using RondiTrack.BackgroundServices;



using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using RondiTrack.Data.Seeding;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Use the system clock for time-dependent background operations.
builder.Services.AddSingleton(TimeProvider.System);

// Automatically close expired monthly contribution cycles.
builder.Services.AddHostedService<MonthlyCycleClosingService>();

// Automatically clean up expired refresh tokens and idempotency records.
builder.Services.AddHostedService<HousekeepingService>();

const string frontendCorsPolicy = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Content-Type", "Authorization", "Idempotency-Key")
                .AllowCredentials()
                .WithExposedHeaders("Retry-After", "Idempotency-Replayed");
        }
    });
});

// ============================================================
// 1. JWT SIGNING KEY VALIDATION
// ============================================================

// Retrieve the JWT signing key from configuration.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing.");

byte[] jwtKeyBytes;

// The signing key must be Base64 encoded.
try
{
    jwtKeyBytes = Convert.FromBase64String(jwtKey);
}
catch (FormatException)
{
    throw new InvalidOperationException(
        "JWT signing key must be Base64.");
}

// Require at least 32 bytes for the signing key.
if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

// ============================================================
// 2. POSTGRESQL DATABASE CONFIGURATION
// ============================================================

// Retrieve the database connection string.
var connectionString =
    builder.Configuration.GetConnectionString("RondiTrack")
    ?? throw new InvalidOperationException(
        "Connection string 'RondiTrack' was not found.");

// Configure EF Core to use PostgreSQL.
builder.Services.AddDbContext<RondiTrackDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            // Retry temporary database connection failures.
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
});

// Control EF Core SQL command logging by environment.
// Development: Information
// Production: Warning
builder.Logging.AddFilter(
    "Microsoft.EntityFrameworkCore.Database.Command",
    builder.Environment.IsDevelopment()
        ? LogLevel.Information
        : LogLevel.Warning);

// ============================================================
// 3. OPENTELEMETRY CONFIGURATION
// ============================================================

// Only export telemetry when an OTLP endpoint is configured.
var otlpEndpoint =
    builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

// Configure OpenTelemetry tracing and metrics.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("ronditrack-api"))

    // Distributed tracing.
    .WithTracing(tracing =>
    {
        tracing
            // Trace incoming HTTP requests.
            .AddAspNetCoreInstrumentation(options =>
{
    options.Filter = context =>
        !context.Request.Path.StartsWithSegments("/health");
})
.AddSource("Npgsql")
.AddSource("RondiTrack.Business");

        // Export traces only when an endpoint exists.
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter();
        }
    })

    // Application metrics.
    .WithMetrics(metrics =>
    {
        // Collect ASP.NET Core request metrics.
        metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter("RondiTrack.Business");

        // Export metrics only when configured.
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter();
        }
    });

// Configure OpenTelemetry logging.
builder.Logging.AddOpenTelemetry(logging =>
{
    // Identify this service in the telemetry dashboard.
    logging.SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService("ronditrack-api"));

    // Include logging scopes.
    logging.IncludeScopes = true;

    // Export logs only when configured.
    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        logging.AddOtlpExporter();
    }
});

// ============================================================
// 4. HEALTH CHECK CONFIGURATION
// ============================================================

// Register application health checks.
// Configure health checks.
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" })
    .AddCheck<RondiTrackDatabaseHealthCheck>(
        "postgresql",
        tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(10));
// ============================================================
// 5. API CONFIGURATION
// ============================================================

// Register OpenAPI document generation services.
builder.Services.AddOpenApi();

// Register controllers and configure strict JSON number handling.
builder.Services.AddControllers(options =>
{
    // Add correlation IDs to controller-generated errors.
    options.Filters.Add<CorrelationIdFilter>();
})
.AddJsonOptions(options =>
{
    // Reject numbers supplied as strings, such as "100" instead of 100.
    options.JsonSerializerOptions.NumberHandling =
        System.Text.Json.Serialization.JsonNumberHandling.Strict;
});

// ============================================================
// 6. JWT AUTHENTICATION
// ============================================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Configure JWT validation.
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                RoleClaimType = ClaimTypes.Role,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(jwtKeyBytes),

                ValidateIssuer = true,
                ValidIssuer = "RondiTrack",

                ValidateAudience = true,
                ValidAudience = "RondiTrack",

                ValidateLifetime = true,

                // Tokens expire at their exact expiration time.
                ClockSkew = TimeSpan.Zero
            };

        // Return Problem Details for authentication failures.
        options.Events = new JwtBearerEvents
        {
            // HTTP 401: No valid authentication.
            OnChallenge = async context =>
            {
                context.HandleResponse();

                var correlationId =
                    Activity.Current?.TraceId.ToString()
                    ?? context.HttpContext.TraceIdentifier;

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Authentication is required.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["correlationId"] = correlationId
                    }
                ).ExecuteAsync(context.HttpContext);
            },

            // HTTP 403: Authenticated but not permitted.
            OnForbidden = async context =>
            {
                var correlationId =
                    Activity.Current?.TraceId.ToString()
                    ?? context.HttpContext.TraceIdentifier;

                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You do not have permission to perform this action.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["correlationId"] = correlationId
                    }
                ).ExecuteAsync(context.HttpContext);
            }
        };
    });

// ============================================================
// 7. AUTHORIZATION POLICIES
// ============================================================

builder.Services.AddAuthorization(options =>
{
    // Require authentication unless an endpoint allows anonymous access.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // User must belong to the requested stokvel.
    options.AddPolicy("StokvelMember", policy =>
        policy.Requirements.Add(
            new StokvelAccessRequirement(false)));

    // User must be Treasurer or Chairperson.
    options.AddPolicy("StokvelManager", policy =>
        policy.Requirements.Add(
            new StokvelAccessRequirement(true)));

    // User must be Chairperson.
    options.AddPolicy("StokvelChairperson", policy =>
        policy.Requirements.Add(
            new StokvelAccessRequirement(
                requireChairperson: true)));
});

// Register the custom authorization handler.
builder.Services.AddScoped<
    IAuthorizationHandler,
    StokvelAccessHandler>();

// ============================================================
// 8. APPLICATION SERVICES
// ============================================================

// Register FluentValidation.
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddValidatorsFromAssemblyContaining<
    CreateUserRequestValidator>();

// Register repository and application services.
builder.Services.AddScoped<
    IRondiTrackRepository,
    EfRondiTrackRepository>();

builder.Services.AddSingleton<
    IIdempotencyStore,
    InMemoryIdempotencyStore>();

builder.Services.AddScoped<
    IRondiTrackService,
    RondiTrackService>();

// ============================================================
// 9. EXCEPTION HANDLING AND PROBLEM DETAILS
// ============================================================

// Register the global exception handler.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add the W3C trace ID to automatically generated problem responses.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] =
            Activity.Current?.TraceId.ToString()
            ?? context.HttpContext.TraceIdentifier;
    };
});

// ============================================================
// 10. VALIDATION ERROR RESPONSES
// ============================================================

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // Use the request's W3C trace ID.
        var correlationId =
            Activity.Current?.TraceId.ToString()
            ?? context.HttpContext.TraceIdentifier;

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        // Log the validation failure without sensitive request values.
        logger.LogInformation(
            "Request validation failed with correlation ID {CorrelationId}",
            correlationId);

        // Collect validation errors using camelCase field names.
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => JsonNamingPolicy.CamelCase.ConvertName(x.Key),
                x => x.Value!.Errors
                    .Select(e => e.ErrorMessage)
                    .ToArray());

        // Create the validation problem response.
        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more request fields are invalid."
        };

        // Include the correlation ID.
        problemDetails.Extensions["correlationId"] =
            correlationId;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes =
            {
                "application/problem+json"
            }
        };
    };
});

// ============================================================
// 11. BUILD APPLICATION
// ============================================================

// Rate limiting policies.
const string loginRateLimit = "Login";
const string financialRateLimit = "Financial";

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        var http = context.HttpContext;

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfter = 60;

        if (context.Lease.TryGetMetadata(
            MetadataName.RetryAfter, out var delay))
        {
            retryAfter = Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds));
        }

        http.Response.Headers.RetryAfter = retryAfter.ToString();

        await Results.Problem(
            statusCode: 429,
            title: "Too many requests",
            detail: "Please retry after the rate limit resets.",
            extensions: new Dictionary<string, object?>
            {
                ["correlationId"] =
                    Activity.Current?.TraceId.ToString()
                    ?? http.TraceIdentifier
            }
        ).ExecuteAsync(http);
    };

    // Limit login attempts by IP address.
    options.AddPolicy(loginRateLimit, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue(
                    "RateLimiting:Login:PermitLimit", 5),
                Window = TimeSpan.FromSeconds(
                    builder.Configuration.GetValue(
                        "RateLimiting:Login:WindowSeconds", 60)),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Allow a burst of 20 financial operations per authenticated user.
    options.AddPolicy(financialRateLimit, context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.User.FindFirstValue(
                JwtRegisteredClaimNames.Sub) ?? "anonymous",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = builder.Configuration.GetValue(
                    "RateLimiting:Financial:TokenLimit", 20),
                TokensPerPeriod = builder.Configuration.GetValue(
                    "RateLimiting:Financial:TokensPerPeriod", 20),
                ReplenishmentPeriod = TimeSpan.FromSeconds(
                    builder.Configuration.GetValue(
                        "RateLimiting:Financial:WindowSeconds", 60)),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// ============================================================
// 12. DEVELOPMENT API DOCUMENTATION
// ============================================================

if (app.Environment.IsDevelopment())
{
    // Allow anonymous access to API documentation.
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

// ============================================================
// 13. MIDDLEWARE
// ============================================================

// Handle application exceptions.
app.UseExceptionHandler();

// Add a W3C trace ID to every request log.
app.Use(async (context, next) =>
{
    var correlationId =
        Activity.Current?.TraceId.ToString()
        ?? context.TraceIdentifier;

    var logger = context.RequestServices
        .GetRequiredService<ILogger<Program>>();

    // Include the trace ID in logs written during this request.
    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        await next();

        // Log the completed request without query strings or request bodies.
        logger.LogInformation(
            "Request completed. Method={Method}, StatusCode={StatusCode}, CorrelationId={CorrelationId}",
            context.Request.Method,
            context.Response.StatusCode,
            correlationId);
    }
});

// Redirect HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Allow configured frontend origins before authentication.
app.UseRouting();
app.UseCors(frontendCorsPolicy);


// Authenticate incoming requests.
app.UseAuthentication();

// Enforce authorization policies.
app.UseRateLimiter();

app.UseAuthorization();


// Map API controllers.
app.MapControllers();

// ============================================================
// 14. HEALTH ENDPOINTS
// ============================================================

// Liveness: confirms that the application is running.
// Does not execute database checks.
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false,

        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "text/plain";

            await context.Response.WriteAsync(
                report.Status == HealthStatus.Healthy
                    ? "Healthy"
                    : "Unhealthy");
        }
    })
    .AllowAnonymous();

// Readiness: checks whether PostgreSQL is accessible.
// Returns only Healthy or Unhealthy, without database details.
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),

        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "text/plain";

            await context.Response.WriteAsync(
                report.Status == HealthStatus.Healthy
                    ? "Healthy"
                    : "Unhealthy");
        }
    })
    .AllowAnonymous();

// ============================================================
// 15. START APPLICATION
// ============================================================

// Seed demonstration records only in the Development environment.
// Only seed demonstration data when explicitly enabled.
// Never run this in Production.
// Development-only demonstration data for Assignment 7.1.
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("SeedDemoData"))
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<RondiTrackDbContext>();

    // Insert demonstration records only if they do not already exist.
    await DevelopmentDataSeeder.SeedAsync(db);

    // Read the demo password from configuration rather than source code.
    var demoPassword = app.Configuration["DemoUserPassword"];

    if (!string.IsNullOrWhiteSpace(demoPassword))
    {
        if (demoPassword.Length < 12)
        {
            throw new InvalidOperationException(
                "DemoUserPassword must contain at least 12 characters.");
        }

        // Find the existing demonstration user.
        var demoUser = await db.Users.FirstOrDefaultAsync(
            user => user.NormalizedEmail ==
                "ASSIGNMENT71-DEMO@RONDITRACK.EXAMPLE");

        if (demoUser is not null &&
            string.IsNullOrWhiteSpace(demoUser.PasswordHash))
        {
            // Use the same password-hashing mechanism as AuthController.
            var passwordHasher =
                new Microsoft.AspNetCore.Identity.PasswordHasher<RondiTrack.Models.User>();

            demoUser.SetPasswordHash(
                passwordHasher.HashPassword(demoUser, demoPassword));

            await db.SaveChangesAsync();
        }
    }
}

// Start the ASP.NET Core application.
app.Run();

// Required for integration tests.
public partial class Program
{
}