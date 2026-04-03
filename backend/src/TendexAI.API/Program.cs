using TendexAI.API.Endpoints;
using Microsoft.IdentityModel.Logging;
using TendexAI.Application;
using TendexAI.Infrastructure;
using TendexAI.API.Endpoints.Auth;
using TendexAI.API.Endpoints.UserManagement;
using TendexAI.API.Endpoints.Rfp;
using TendexAI.API.Endpoints.Committees;
using TendexAI.API.Endpoints.Evaluation;
using TendexAI.API.Endpoints.AI;
using TendexAI.API.Endpoints.Impersonation;
using TendexAI.API.Endpoints.OperatorDashboard;
using TendexAI.API.Endpoints.Dashboard;
using TendexAI.API.Endpoints.Tasks;
using TendexAI.API.Endpoints.Notifications;
using TendexAI.API.Endpoints.Reports;
using TendexAI.API.Endpoints.Inquiries;
using TendexAI.API.Endpoints.Workflow;
using TendexAI.API.Endpoints.PermissionMatrix;

// Enable PII logging for debugging JWT validation issues (TEMPORARY)
IdentityModelEventSource.ShowPII = true;

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

// ---------------------------------------------------------------------------
// CORS Configuration (TASK-905)
// ---------------------------------------------------------------------------
// Allows the frontend SPA to communicate with the backend API.
// In production, Nginx handles CORS via reverse proxy, but this ensures
// direct API access also works (e.g., development, mobile apps).
// ---------------------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("TendexCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

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

// CORS middleware (TASK-905) — must be before Authentication
app.UseCors("TendexCorsPolicy");

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

// Competition Template management endpoints
app.MapCompetitionTemplateEndpoints();

// Committee management endpoints
app.MapCommitteeEndpoints();

// Supplier Offer management endpoints
app.MapSupplierOfferEndpoints();

// Technical Evaluation endpoints
app.MapTechnicalEvaluationEndpoints();

// Financial Evaluation endpoints
app.MapFinancialEvaluationEndpoints();

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

// AI Booklet Extraction endpoints (Upload & Extract feature)
app.MapBookletExtractionEndpoints();

// AI Text Assistant endpoints (general-purpose text generation/improvement)
app.MapAiTextAssistEndpoints();

// AI User Management Assistant endpoints (role suggestions, permission analysis)
app.MapAiUserManagementEndpoints();

// Impersonation endpoints (Super Admin only - TASK-603)
app.MapImpersonationEndpoints();

// Operator Dashboard endpoints (Super Admin only - TASK-602)
app.MapOperatorDashboardEndpoints();

// Dashboard endpoints (Tenant user dashboard - TASK-901)
app.MapDashboardEndpoints();

// Task center endpoints (Pending tasks - TASK-901)
app.MapTaskEndpoints();

// Notification endpoints (User notifications - TASK-901)
app.MapNotificationEndpoints();

// Report endpoints (Analytics & reporting - TASK-904)
app.MapReportEndpoints();

// Inquiry endpoints (Specification booklet inquiries - TASK-904)
app.MapInquiryEndpoints();

// Booklet Template endpoints (EXPRO official templates with color-coded editor)
app.MapBookletTemplateEndpoints();

// Approval Workflow endpoints (workflow engine & definitions)
app.MapApprovalWorkflowEndpoints();

// Award Recommendation endpoints (final ranking, generate, approve, reject)
app.MapAwardEndpoints();

// Permission Matrix endpoints (flexible N-dimensional permission matrix)
app.MapPermissionMatrixEndpoints();

// Support Ticket endpoints (tenant-operator support system with AI)
app.MapSupportTicketEndpoints();

app.Run();
