using RondiTrack.Repositories;
using Scalar.AspNetCore;
using RondiTrack.Services;
using RondiTrack.Idempotency;
using FluentValidation;
using FluentValidation.AspNetCore;
using RondiTrack.Validators;
using RondiTrack.ExceptionHandling;
using RondiTrack.Authorization;
using Microsoft.AspNetCore.Mvc;
using RondiTrack.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Validate the JWT signing key.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");

byte[] jwtKeyBytes;

try
{
    jwtKeyBytes = Convert.FromBase64String(jwtKey);
}
catch (FormatException)
{
    throw new InvalidOperationException("JWT signing key must be Base64.");
}

if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

// Configure PostgreSQL.
var connectionString =
    builder.Configuration.GetConnectionString("RondiTrack")
    ?? throw new InvalidOperationException(
        "Connection string 'RondiTrack' was not found.");

builder.Services.AddDbContext<RondiTrackDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure JWT authentication.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = ClaimTypes.Role,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),

            ValidateIssuer = true,
            ValidIssuer = "RondiTrack",

            ValidateAudience = true,
            ValidAudience = "RondiTrack",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero

        };

        // Return Problem Details when authentication fails.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Authentication is required."
                ).ExecuteAsync(context.HttpContext);
            },

            OnForbidden = async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You do not have permission to perform this action."
                ).ExecuteAsync(context.HttpContext);
            }
        };
    });

// Configure authorization policies.
builder.Services.AddAuthorization(options =>
{
    // Secure endpoints by default.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Requires membership in the requested stokvel.
    options.AddPolicy("StokvelMember", policy =>
        policy.Requirements.Add(
            new StokvelAccessRequirement(false)));

    // Requires Treasurer or Chairperson membership.
    options.AddPolicy("StokvelManager", policy =>
        policy.Requirements.Add(
            new StokvelAccessRequirement(true)));

    options.AddPolicy("StokvelChairperson", policy =>
        policy.Requirements.Add(new StokvelAccessRequirement(
            requireChairperson: true)));
});

// Register the custom authorization handler.
builder.Services.AddScoped<IAuthorizationHandler, StokvelAccessHandler>();

// Register application services.
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

builder.Services.AddScoped<IRondiTrackRepository, EfRondiTrackRepository>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddScoped<IRondiTrackService, RondiTrackService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure validation error responses.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var correlationId =
            context.HttpContext.TraceIdentifier;

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogWarning(
            "Request validation failed with correlation ID {CorrelationId}",
            correlationId);

        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors
                    .Select(e => e.ErrorMessage)
                    .ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more request fields are invalid."
        };

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

var app = builder.Build();

// Configure OpenAPI and Scalar in development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

// Configure middleware.
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}