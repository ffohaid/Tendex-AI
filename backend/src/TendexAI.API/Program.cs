using TendexAI.Application;
using TendexAI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service Registration (Dependency Injection)
// ---------------------------------------------------------------------------

// Register Application layer services (MediatR, FluentValidation, etc.)
builder.Services.AddApplicationServices();

// Register Infrastructure layer services (EF Core, Redis, MinIO, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// OpenAPI / Swagger documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware Pipeline
// ---------------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ---------------------------------------------------------------------------
// Minimal API Endpoints
// ---------------------------------------------------------------------------

app.MapGet("/api/v1/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Service = "TendexAI.API"
}))
.WithName("HealthCheck")
.WithTags("System");

app.Run();
