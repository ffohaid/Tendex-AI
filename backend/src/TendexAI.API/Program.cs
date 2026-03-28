using TendexAI.API.Endpoints;
using TendexAI.Application;
using TendexAI.Infrastructure;
using TendexAI.API.Endpoints.Auth;
using TendexAI.API.Endpoints.UserManagement;
using TendexAI.API.Endpoints.Rfp;
using TendexAI.API.Endpoints.Committees;
using TendexAI.API.Endpoints.Evaluation;
using TendexAI.API.Endpoints.AI;
using TendexAI.API.Endpoints.Impersonation;

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

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

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

// Authentication endpoints
app.MapAuthEndpoints();

app.MapGet("/api/v1/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Service = "TendexAI.API",
    Version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("System")
.AllowAnonymous();

// Audit Trail Endpoints (Read-Only)
app.MapAuditTrailEndpoints();

// File management endpoints
app.MapFileEndpoints();

// User management endpoints
app.MapUserManagementEndpoints();

// Tenant (Government Entity) lifecycle management endpoints
app.MapTenantEndpoints();

// Feature flag management endpoints
app.MapFeatureFlagEndpoints();

// Competition (RFP) management endpoints
app.MapCompetitionEndpoints();

// Committee management endpoints
app.MapCommitteeEndpoints();

// Technical Evaluation endpoints
app.MapTechnicalEvaluationEndpoints();

// AI Offer Analysis endpoints (AI-powered technical offer analysis & recommendations)
app.MapAiOfferAnalysisEndpoints();

// AI Configuration management endpoints
app.MapAiConfigurationEndpoints();

// AI Gateway endpoints (completions, embeddings, status)
app.MapAiGatewayEndpoints();

// Video Integrity Analysis endpoints (tamper detection, identity verification)
app.MapVideoIntegrityEndpoints();

// RAG Engine endpoints (indexing, context retrieval, vector store management)
app.MapRagEndpoints();

// AI Specification Drafting & BOQ Generation endpoints (TASK-403)
app.MapAiSpecificationDraftingEndpoints();

// Impersonation endpoints (Super Admin only - TASK-603)
app.MapImpersonationEndpoints();

app.Run();
