using TendexAI.API.Endpoints;
using TendexAI.Application;
using TendexAI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service Registration (Dependency Injection)
// ---------------------------------------------------------------------------

// Register Application layer services (MediatR, FluentValidation, etc.)
builder.Services.AddApplicationServices();

// Register Infrastructure layer services (EF Core, MinIO, Redis, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// OpenAPI / Swagger documentation
builder.Services.AddOpenApi();

// Configure maximum request body size for file uploads (50 MB default)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52_428_800; // 50 MB
});

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
// Health Check Endpoints
// ---------------------------------------------------------------------------

app.MapHealthChecks("/api/v1/health/storage", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("storage")
});

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

// Audit Trail Endpoints (Read-Only)
app.MapAuditTrailEndpoints();

// File management endpoints
app.MapFileEndpoints();

app.Run();
