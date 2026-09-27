using RondiTrack.Repositories;
using Scalar.AspNetCore;
using RondiTrack.Services;
using RondiTrack.Idempotency;
using FluentValidation;
using FluentValidation.AspNetCore;
using RondiTrack.Validators;
using RondiTrack.ExceptionHandling;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
builder.Services.AddSingleton<IRondiTrackRepository, InMemoryRondiTrackRepository>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddSingleton<IRondiTrackService, RondiTrackService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var correlationId =
            context.HttpContext.TraceIdentifier;

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

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


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Use the global exception handler middleware
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapControllers();

// Run the application
app.Run();

public partial class Program
{

}