using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Committees;

/// <summary>
/// Unit tests for the Committee aggregate root entity.
/// Covers creation, member management, lifecycle transitions, and business rules.
/// </summary>
public sealed class CommitteeTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _competitionId = Guid.NewGuid();
    private readonly string _createdBy = Guid.NewGuid().ToString();

    private Committee CreateTestCommittee(
        CommitteeType type = CommitteeType.TechnicalEvaluation,
        bool isPermanent = false)
    {
        return new Committee(
            tenantId: _tenantId,
            nameAr: "لجنة الفحص الفني",
            nameEn: "Technical Evaluation Committee",
            type: type,
            isPermanent: isPermanent,
            description: "Test committee",
            startDate: DateTime.UtcNow,
            endDate: DateTime.UtcNow.AddMonths(6),
            competitionId: isPermanent ? null : _competitionId,
            activeFromPhase: CompetitionPhase.TechnicalAnalysis,
            activeToPhase: CompetitionPhase.TechnicalAnalysis,
            createdBy: _createdBy);
    }

    // ═════════════════════════════════════════════════════════════
    //  Creation Tests
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_ShouldCreateCommittee_WithCorrectProperties()
    {
        // Act
        var committee = CreateTestCommittee();

        // Assert
        Assert.NotEqual(Guid.Empty, committee.Id);
        Assert.Equal(_tenantId, committee.TenantId);
        Assert.Equal("لجنة الفحص الفني", committee.NameAr);
        Assert.Equal("Technical Evaluation Committee", committee.NameEn);
        Assert.Equal(CommitteeType.TechnicalEvaluation, committee.Type);
        Assert.False(committee.IsPermanent);
        Assert.Equal(CommitteeStatus.Active, committee.Status);
        Assert.Equal(_competitionId, committee.CompetitionId);
        Assert.Equal(_createdBy, committee.CreatedBy);
        Assert.Empty(committee.Members);
    }

    [Fact]
    public void Constructor_PermanentCommittee_ShouldHaveNoCompetitionId()
    {
        // Act
        var committee = CreateTestCommittee(isPermanent: true);

        // Assert
        Assert.True(committee.IsPermanent);
        Assert.Null(committee.CompetitionId);
    }

    // ═════════════════════════════════════════════════════════════
    //  Member Management Tests
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void AddMember_ShouldSucceed_WhenCommitteeIsActive()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var userId = Guid.NewGuid();

        // Act
        var result = committee.AddMember(
            userId, "Ahmed Ali", CommitteeMemberRole.Member,
            null, null, _createdBy);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(committee.Members);
        Assert.Equal(1, committee.ActiveMemberCount);
    }

    [Fact]
    public void AddMember_ShouldFail_WhenCommitteeIsSuspended()
    {
        // Arrange
        var committee = CreateTestCommittee();
        committee.Suspend(_createdBy, "Test suspension");

        // Act
        var result = committee.AddMember(
            Guid.NewGuid(), "Ahmed Ali", CommitteeMemberRole.Member,
            null, null, _createdBy);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("non-active", result.Error!);
    }

    [Fact]
    public void AddMember_ShouldFail_WhenDuplicateActiveMember()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var userId = Guid.NewGuid();
        committee.AddMember(userId, "Ahmed Ali", CommitteeMemberRole.Member, null, null, _createdBy);

        // Act
        var result = committee.AddMember(
            userId, "Ahmed Ali", CommitteeMemberRole.Secretary, null, null, _createdBy);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already an active member", result.Error!);
    }

    [Fact]
    public void AddMember_ShouldFail_WhenSecondChairAdded()
    {
        // Arrange
        var committee = CreateTestCommittee();
        committee.AddMember(Guid.NewGuid(), "Chair 1", CommitteeMemberRole.Chair, null, null, _createdBy);

        // Act
        var result = committee.AddMember(
            Guid.NewGuid(), "Chair 2", CommitteeMemberRole.Chair, null, null, _createdBy);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already has an active chair", result.Error!);
    }

    [Fact]
    public void RemoveMember_ShouldSucceed_WithValidReason()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var userId = Guid.NewGuid();
        committee.AddMember(userId, "Ahmed Ali", CommitteeMemberRole.Member, null, null, _createdBy);

        // Act
        var result = committee.RemoveMember(userId, _createdBy, "No longer available");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, committee.ActiveMemberCount);
    }

    [Fact]
    public void RemoveMember_ShouldFail_WithoutReason()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var userId = Guid.NewGuid();
        committee.AddMember(userId, "Ahmed Ali", CommitteeMemberRole.Member, null, null, _createdBy);

        // Act
        var result = committee.RemoveMember(userId, _createdBy, "");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("reason is required", result.Error!);
    }

    [Fact]
    public void RemoveMember_ShouldFail_WhenMemberNotFound()
    {
        // Arrange
        var committee = CreateTestCommittee();

        // Act
        var result = committee.RemoveMember(Guid.NewGuid(), _createdBy, "Test reason");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public void HasActiveMember_ShouldReturnTrue_ForActiveMember()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var userId = Guid.NewGuid();
        committee.AddMember(userId, "Ahmed Ali", CommitteeMemberRole.Member, null, null, _createdBy);

        // Act & Assert
        Assert.True(committee.HasActiveMember(userId));
    }

    [Fact]
    public void IsChair_ShouldReturnTrue_ForActiveChair()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var userId = Guid.NewGuid();
        committee.AddMember(userId, "Chair", CommitteeMemberRole.Chair, null, null, _createdBy);

        // Act & Assert
        Assert.True(committee.IsChair(userId));
    }

    // ═════════════════════════════════════════════════════════════
    //  Lifecycle Tests
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void Suspend_ShouldSucceed_WhenActive()
    {
        // Arrange
        var committee = CreateTestCommittee();

        // Act
        var result = committee.Suspend(_createdBy, "Budget review");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CommitteeStatus.Suspended, committee.Status);
        Assert.Equal("Budget review", committee.StatusChangeReason);
    }

    [Fact]
    public void Suspend_ShouldFail_WhenAlreadySuspended()
    {
        // Arrange
        var committee = CreateTestCommittee();
        committee.Suspend(_createdBy, "First suspension");

        // Act
        var result = committee.Suspend(_createdBy, "Second suspension");

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Reactivate_ShouldSucceed_WhenSuspended()
    {
        // Arrange
        var committee = CreateTestCommittee();
        committee.Suspend(_createdBy, "Temporary suspension");

        // Act
        var result = committee.Reactivate(_createdBy);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CommitteeStatus.Active, committee.Status);
    }

    [Fact]
    public void Dissolve_ShouldDeactivateAllMembers()
    {
        // Arrange
        var committee = CreateTestCommittee();
        committee.AddMember(Guid.NewGuid(), "Member 1", CommitteeMemberRole.Chair, null, null, _createdBy);
        committee.AddMember(Guid.NewGuid(), "Member 2", CommitteeMemberRole.Member, null, null, _createdBy);
        committee.AddMember(Guid.NewGuid(), "Member 3", CommitteeMemberRole.Secretary, null, null, _createdBy);

        // Act
        var result = committee.Dissolve(_createdBy, "Competition cancelled");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CommitteeStatus.Dissolved, committee.Status);
        Assert.Equal(0, committee.ActiveMemberCount);
        Assert.All(committee.Members, m => Assert.False(m.IsActive));
    }

    [Fact]
    public void ExtendTerm_ShouldSucceed_WithLaterDate()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var newEndDate = committee.EndDate.AddMonths(3);

        // Act
        var result = committee.ExtendTerm(newEndDate, _createdBy);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newEndDate, committee.EndDate);
    }

    [Fact]
    public void ExtendTerm_ShouldFail_WithEarlierDate()
    {
        // Arrange
        var committee = CreateTestCommittee();
        var earlierDate = committee.EndDate.AddDays(-1);

        // Act
        var result = committee.ExtendTerm(earlierDate, _createdBy);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("after the current end date", result.Error!);
    }

    [Fact]
    public void UpdateInfo_ShouldSucceed_WhenNotDissolved()
    {
        // Arrange
        var committee = CreateTestCommittee();

        // Act
        var result = committee.UpdateInfo("اسم جديد", "New Name", "New description", _createdBy);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("اسم جديد", committee.NameAr);
        Assert.Equal("New Name", committee.NameEn);
    }

    [Fact]
    public void UpdateInfo_ShouldFail_WhenDissolved()
    {
        // Arrange
        var committee = CreateTestCommittee();
        committee.Dissolve(_createdBy, "Test dissolution");

        // Act
        var result = committee.UpdateInfo("اسم جديد", "New Name", null, _createdBy);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("dissolved", result.Error!);
    }
}
