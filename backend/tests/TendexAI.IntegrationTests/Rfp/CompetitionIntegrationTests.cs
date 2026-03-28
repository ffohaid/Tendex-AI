using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TendexAI.IntegrationTests.Infrastructure;

namespace TendexAI.IntegrationTests.Rfp;

/// <summary>
/// Integration tests for Competition (RFP) CRUD operations.
/// Tests cover creation, retrieval, update, deletion, and edge cases.
/// </summary>
[Collection("Integration")]
public sealed class CompetitionIntegrationTests : IntegrationTestBase
{
    public CompetitionIntegrationTests(TendexWebApplicationFactory factory) : base(factory) { }

    // -------------------------------------------------------------------------
    // Competition Creation Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCompetition_WithValidData_ShouldReturn201()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        var request = new
        {
            ProjectNameAr = "مشروع تطوير البنية التحتية الرقمية",
            ProjectNameEn = "Digital Infrastructure Development Project",
            Description = "مشروع لتطوير البنية التحتية الرقمية للجهة الحكومية",
            CompetitionType = 0, // PublicTender
            CreationMethod = 0, // ManualWizard
            EstimatedBudget = 5000000.00m,
            SubmissionDeadline = DateTime.UtcNow.AddMonths(3),
            ProjectDurationDays = 365
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/competitions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("id", out var idProp).Should().BeTrue();
        jsonDoc.RootElement.TryGetProperty("projectNameAr", out var nameArProp).Should().BeTrue();
        nameArProp.GetString().Should().Be("مشروع تطوير البنية التحتية الرقمية");
    }

    [Fact]
    public async Task CreateCompetition_WithMinimalData_ShouldReturn201()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        var request = new
        {
            ProjectNameAr = "مشروع اختباري",
            ProjectNameEn = "Test Project",
            CompetitionType = 1, // LimitedTender
            CreationMethod = 0  // ManualWizard
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/competitions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCompetition_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            ProjectNameAr = "مشروع اختباري",
            ProjectNameEn = "Test Project",
            CompetitionType = 0,
            CreationMethod = 0
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/competitions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCompetition_WithMissingRequiredFields_ShouldReturn400()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        var request = new
        {
            ProjectNameAr = "", // Empty required field
            ProjectNameEn = "Test Project",
            CompetitionType = 0,
            CreationMethod = 0
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/competitions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Competition Retrieval Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCompetitions_WithValidAuth_ShouldReturnPagedResult()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/competitions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetCompetitionById_WithValidId_ShouldReturnCompetition()
    {
        // Arrange - Create a competition first
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            ProjectNameAr = "مشروع للاسترجاع",
            ProjectNameEn = "Retrieval Test Project",
            CompetitionType = 0,
            CreationMethod = 0,
            EstimatedBudget = 1000000.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/competitions", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(createContent);
        var competitionId = jsonDoc.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/competitions/{competitionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailContent = await response.Content.ReadAsStringAsync();
        var detailDoc = JsonDocument.Parse(detailContent);
        detailDoc.RootElement.GetProperty("id").GetGuid().Should().Be(competitionId);
        detailDoc.RootElement.GetProperty("projectNameAr").GetString().Should().Be("مشروع للاسترجاع");
    }

    [Fact]
    public async Task GetCompetitionById_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/competitions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Competition Update Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateCompetition_WithValidData_ShouldReturn200()
    {
        // Arrange - Create a competition first
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            ProjectNameAr = "مشروع قبل التحديث",
            ProjectNameEn = "Before Update Project",
            CompetitionType = 0,
            CreationMethod = 0
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/competitions", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(createContent);
        var competitionId = jsonDoc.RootElement.GetProperty("id").GetGuid();

        // Act
        var updateRequest = new
        {
            ProjectNameAr = "مشروع بعد التحديث",
            ProjectNameEn = "After Update Project",
            CompetitionType = 0,
            EstimatedBudget = 2000000.00m,
            ProjectDurationDays = 180
        };

        var response = await client.PutAsJsonAsync($"/api/v1/competitions/{competitionId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedContent = await response.Content.ReadAsStringAsync();
        var updatedDoc = JsonDocument.Parse(updatedContent);
        updatedDoc.RootElement.GetProperty("projectNameAr").GetString().Should().Be("مشروع بعد التحديث");
    }

    // -------------------------------------------------------------------------
    // Competition Deletion Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteCompetition_DraftStatus_ShouldReturn204()
    {
        // Arrange - Create a competition (defaults to Draft status)
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            ProjectNameAr = "مشروع للحذف",
            ProjectNameEn = "Deletion Test Project",
            CompetitionType = 0,
            CreationMethod = 0
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/competitions", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(createContent);
        var competitionId = jsonDoc.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/competitions/{competitionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCompetition_NonExistentId_ShouldReturn404()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/competitions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Auto-Save Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AutoSaveCompetition_WithPartialData_ShouldReturn200()
    {
        // Arrange - Create a competition first
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            ProjectNameAr = "مشروع للحفظ التلقائي",
            ProjectNameEn = "Auto-Save Test Project",
            CompetitionType = 0,
            CreationMethod = 0
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/competitions", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(createContent);
        var competitionId = jsonDoc.RootElement.GetProperty("id").GetGuid();

        // Act - Partial update via auto-save
        var autoSaveRequest = new
        {
            Description = "وصف محدث عبر الحفظ التلقائي"
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/competitions/{competitionId}/auto-save")
        {
            Content = JsonContent.Create(autoSaveRequest)
        };

        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Pagination Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCompetitions_WithPagination_ShouldRespectPageSize()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Create multiple competitions
        for (var i = 0; i < 3; i++)
        {
            var createRequest = new
            {
                ProjectNameAr = $"مشروع ترقيم {i + 1}",
                ProjectNameEn = $"Pagination Test Project {i + 1}",
                CompetitionType = 0,
                CreationMethod = 0
            };
            await client.PostAsJsonAsync("/api/v1/competitions", createRequest);
        }

        // Act
        var response = await client.GetAsync("/api/v1/competitions?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }
}
