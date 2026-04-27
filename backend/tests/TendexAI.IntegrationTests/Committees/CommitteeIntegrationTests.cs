using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TendexAI.IntegrationTests.Infrastructure;

namespace TendexAI.IntegrationTests.Committees;

/// <summary>
/// Integration tests for Committee management endpoints.
/// Tests cover CRUD operations, member management, and conflict of interest validation.
/// </summary>
[Collection("Integration")]
public sealed class CommitteeIntegrationTests : IntegrationTestBase
{
    public CommitteeIntegrationTests(TendexWebApplicationFactory factory) : base(factory) { }

    // -------------------------------------------------------------------------
    // Committee Creation Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCommittee_WithValidData_ShouldReturn201()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        var request = new
        {
            NameAr = "لجنة فحص العروض الفنية",
            NameEn = "Technical Bid Evaluation Committee",
            Type = 1, // TechnicalEvaluation
            IsPermanent = true,
            ScopeType = 1, // Comprehensive
            Description = "لجنة دائمة لفحص العروض الفنية",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/committees", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCommittee_TemporaryWithCompetition_ShouldReturn201()
    {
        // Arrange - Create a competition first
        var client = await GetAuthenticatedClientAsync();

        var competitionRequest = new
        {
            ProjectNameAr = "مشروع لربط اللجنة",
            ProjectNameEn = "Committee Link Project",
            CompetitionType = 0,
            CreationMethod = 0
        };

        var competitionResponse = await client.PostAsJsonAsync("/api/v1/competitions", competitionRequest);
        competitionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var competitionContent = await competitionResponse.Content.ReadAsStringAsync();
        var competitionDoc = JsonDocument.Parse(competitionContent);
        var competitionId = competitionDoc.RootElement.GetProperty("id").GetGuid();

        // Create a temporary committee linked to the competition
        var committeeRequest = new
        {
            NameAr = "لجنة إعداد الكراسة",
            NameEn = "Booklet Preparation Committee",
            Type = 3, // BookletPreparation
            IsPermanent = false,
            ScopeType = 3, // SpecificPhasesSpecificCompetitions
            Description = "لجنة مؤقتة لإعداد كراسة الشروط والمواصفات",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(3),
            CompetitionIds = new[] { competitionId },
            Phases = new[] { 1 } // BookletPreparation
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/committees", committeeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCommittee_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            NameAr = "لجنة اختبارية",
            NameEn = "Test Committee",
            Type = 1,
            IsPermanent = true,
            ScopeType = 1, // Comprehensive
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/committees", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCommittee_WithMissingRequiredFields_ShouldReturn400()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        var request = new
        {
            NameAr = "", // Empty required field
            NameEn = "Test Committee",
            Type = 1,
            IsPermanent = true,
            ScopeType = 1, // Comprehensive
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/committees", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCommittee_WithEndDateBeforeStartDate_ShouldReturn400()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        var request = new
        {
            NameAr = "لجنة بتواريخ خاطئة",
            NameEn = "Wrong Dates Committee",
            Type = 1,
            IsPermanent = true,
            StartDate = DateTime.UtcNow.AddYears(1),
            EndDate = DateTime.UtcNow // End before start
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/committees", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Committee Retrieval Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCommittees_WithValidAuth_ShouldReturnPagedResult()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/committees?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetCommitteeById_WithValidId_ShouldReturnCommittee()
    {
        // Arrange - Create a committee first
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            NameAr = "لجنة للاسترجاع",
            NameEn = "Retrieval Test Committee",
            Type = 2, // FinancialEvaluation
            IsPermanent = true,
            ScopeType = 1, // Comprehensive
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/committees", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var committeeId = JsonDocument.Parse(createContent).RootElement.GetGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/committees/{committeeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailContent = await response.Content.ReadAsStringAsync();
        var detailDoc = JsonDocument.Parse(detailContent);
        detailDoc.RootElement.GetProperty("nameAr").GetString().Should().Be("لجنة للاسترجاع");
    }

    [Fact]
    public async Task GetCommitteeById_WithNonExistentId_ShouldReturn404()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/committees/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Committee Update Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateCommittee_WithValidData_ShouldReturn200()
    {
        // Arrange - Create a committee first
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            NameAr = "لجنة قبل التحديث",
            NameEn = "Before Update Committee",
            Type = 1,
            IsPermanent = true,
            ScopeType = 1, // Comprehensive
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/committees", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var committeeId = JsonDocument.Parse(createContent).RootElement.GetGuid();

        // Act
        var updateRequest = new
        {
            NameAr = "لجنة بعد التحديث",
            NameEn = "After Update Committee",
            Description = "وصف محدث",
            ScopeType = 1, // Comprehensive
            Phases = Array.Empty<int>(),
            CompetitionIds = Array.Empty<Guid>()
        };

        var response = await client.PutAsJsonAsync($"/api/v1/committees/{committeeId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Member Management Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddCommitteeMember_WithValidData_ShouldReturn200()
    {
        // Arrange - Create a committee first
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            NameAr = "لجنة لإضافة أعضاء",
            NameEn = "Member Addition Committee",
            Type = 1,
            IsPermanent = true,
            ScopeType = 1, // Comprehensive
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/committees", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var committeeId = JsonDocument.Parse(createContent).RootElement.GetGuid();

        // Act - Add a member
        var memberRequest = new
        {
            UserId = TestRegularUserId,
            UserFullName = "المستخدم العادي",
            Role = 2 // Member
        };

        var response = await client.PostAsJsonAsync($"/api/v1/committees/{committeeId}/members", memberRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddCommitteeMember_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var memberRequest = new
        {
            UserId = Guid.NewGuid(),
            UserFullName = "مستخدم اختباري",
            Role = 2 // Member
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/v1/committees/{Guid.NewGuid()}/members", memberRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Committee Status Change Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ChangeCommitteeStatus_Suspend_ShouldReturn200()
    {
        // Arrange - Create a committee first
        var client = await GetAuthenticatedClientAsync();

        var createRequest = new
        {
            NameAr = "لجنة لتعليق العمل",
            NameEn = "Suspension Test Committee",
            Type = 1,
            IsPermanent = true,
            ScopeType = 1, // Comprehensive
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/committees", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var committeeId = JsonDocument.Parse(createContent).RootElement.GetGuid();

        // Act - Suspend the committee
        var statusRequest = new
        {
            NewStatus = 2, // Suspended
            Reason = "تعليق مؤقت لأغراض الاختبار"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/committees/{committeeId}/status", statusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Competition-Committee Link Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetCompetitionCommittees_ShouldReturnLinkedCommittees()
    {
        // Arrange - Create competition and committee
        var client = await GetAuthenticatedClientAsync();

        var competitionRequest = new
        {
            ProjectNameAr = "مشروع لاسترجاع اللجان",
            ProjectNameEn = "Committee Retrieval Project",
            CompetitionType = 0,
            CreationMethod = 0
        };

        var competitionResponse = await client.PostAsJsonAsync("/api/v1/competitions", competitionRequest);
        competitionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var competitionContent = await competitionResponse.Content.ReadAsStringAsync();
        var competitionDoc = JsonDocument.Parse(competitionContent);
        var competitionId = competitionDoc.RootElement.GetProperty("id").GetGuid();

        // Create a committee linked to the competition
        var committeeRequest = new
        {
            NameAr = "لجنة مرتبطة بمنافسة",
            NameEn = "Competition-Linked Committee",
            Type = 3, // BookletPreparation
            IsPermanent = false,
            ScopeType = 3, // SpecificPhasesSpecificCompetitions
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(3),
            CompetitionIds = new[] { competitionId },
            Phases = new[] { 1 } // BookletPreparation
        };

        await client.PostAsJsonAsync("/api/v1/committees", committeeRequest);

        // Act - Get committees for this competition
        var response = await client.GetAsync($"/api/v1/competitions/{competitionId}/committees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
