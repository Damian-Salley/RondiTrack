using RondiTrack.Repositories;
using Scalar.AspNetCore;
using RondiTrack.Services;
using RondiTrack.Idempotency;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<IRondiTrackRepository, InMemoryRondiTrackRepository>();
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddSingleton<IRondiTrackService, RondiTrackService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapControllers();

// Run the application
app.Run();