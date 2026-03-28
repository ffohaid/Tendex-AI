using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Evaluation;

/// <summary>
/// Unit tests for the TechnicalEvaluation aggregate root.
/// Validates lifecycle transitions, blind evaluation, scoring, and approval workflow.
/// </summary>
public sealed class TechnicalEvaluationTests
{
    private readonly Guid _competitionId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _committeeId = Guid.NewGuid();
    private const decimal MinPassingScore = 60m;
    private const string UserId = "user-001";

    private TechnicalEvaluation CreateEvaluation()
    {
        return TechnicalEvaluation.Create(
            _competitionId, _tenantId, _committeeId, MinPassingScore, UserId);
    }

    // ═══════════════════════════════════════════════════════════
    //  Creation Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Create_Should_Initialize_With_Pending_Status()
    {
        var evaluation = CreateEvaluation();

        evaluation.Status.Should().Be(TechnicalEvaluationStatus.Pending);
        evaluation.CompetitionId.Should().Be(_competitionId);
        evaluation.TenantId.Should().Be(_tenantId);
        evaluation.CommitteeId.Should().Be(_committeeId);
        evaluation.MinimumPassingScore.Should().Be(MinPassingScore);
        evaluation.IsBlindEvaluationActive.Should().BeTrue();
        evaluation.StartedAt.Should().BeNull();
        evaluation.ApprovedAt.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    //  Start Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Start_Should_Transition_To_InProgress()
    {
        var evaluation = CreateEvaluation();

        var result = evaluation.Start(UserId);

        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(TechnicalEvaluationStatus.InProgress);
        evaluation.StartedAt.Should().NotBeNull();
        evaluation.IsBlindEvaluationActive.Should().BeTrue();
    }

    [Fact]
    public void Start_Should_Fail_If_Not_Pending()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var result = evaluation.Start(UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    // ═══════════════════════════════════════════════════════════
    //  Scoring Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void AddScore_Should_Succeed_When_InProgress()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var score = TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, "Good", "evaluator-1");

        var result = evaluation.AddScore(score);

        result.IsSuccess.Should().BeTrue();
        evaluation.Scores.Should().HaveCount(1);
    }

    [Fact]
    public void AddScore_Should_Fail_When_Not_InProgress()
    {
        var evaluation = CreateEvaluation();

        var score = TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1");

        var result = evaluation.AddScore(score);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("in progress");
    }

    [Fact]
    public void AddScore_Should_Prevent_Duplicate_Evaluator_Criterion_Offer()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var offerId = Guid.NewGuid();
        var criterionId = Guid.NewGuid();

        var score1 = TechnicalScore.Create(
            evaluation.Id, offerId, criterionId,
            "evaluator-1", 80m, 100m, null, "evaluator-1");

        var score2 = TechnicalScore.Create(
            evaluation.Id, offerId, criterionId,
            "evaluator-1", 90m, 100m, null, "evaluator-1");

        evaluation.AddScore(score1);
        var result = evaluation.AddScore(score2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public void AddScore_Should_Allow_Different_Evaluators_Same_Criterion()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var offerId = Guid.NewGuid();
        var criterionId = Guid.NewGuid();

        var score1 = TechnicalScore.Create(
            evaluation.Id, offerId, criterionId,
            "evaluator-1", 80m, 100m, null, "evaluator-1");

        var score2 = TechnicalScore.Create(
            evaluation.Id, offerId, criterionId,
            "evaluator-2", 85m, 100m, null, "evaluator-2");

        evaluation.AddScore(score1);
        var result = evaluation.AddScore(score2);

        result.IsSuccess.Should().BeTrue();
        evaluation.Scores.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateScore_Should_Only_Allow_Original_Evaluator()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var score = TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1");

        evaluation.AddScore(score);

        var result = evaluation.UpdateScore(score.Id, 90m, "Updated", "evaluator-2");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("original evaluator");
    }

    // ═══════════════════════════════════════════════════════════
    //  Completion & Approval Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void MarkAllScoresSubmitted_Should_Fail_Without_Scores()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var result = evaluation.MarkAllScoresSubmitted(UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no scores");
    }

    [Fact]
    public void MarkAllScoresSubmitted_Should_Succeed_With_Scores()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var score = TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1");
        evaluation.AddScore(score);

        var result = evaluation.MarkAllScoresSubmitted(UserId);

        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(TechnicalEvaluationStatus.AllScoresSubmitted);
        evaluation.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void SubmitForApproval_Should_Transition_From_AllScoresSubmitted()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1"));
        evaluation.MarkAllScoresSubmitted(UserId);

        var result = evaluation.SubmitForApproval(UserId);

        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(TechnicalEvaluationStatus.PendingApproval);
    }

    [Fact]
    public void ApproveReport_Should_Reveal_Identities_And_Set_Status()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1"));
        evaluation.MarkAllScoresSubmitted(UserId);
        evaluation.SubmitForApproval(UserId);

        var result = evaluation.ApproveReport("chair-001");

        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(TechnicalEvaluationStatus.Approved);
        evaluation.IsBlindEvaluationActive.Should().BeFalse();
        evaluation.ApprovedAt.Should().NotBeNull();
        evaluation.ApprovedBy.Should().Be("chair-001");
        evaluation.IsReportApproved.Should().BeTrue();
    }

    [Fact]
    public void ApproveReport_Should_Fail_If_Not_PendingApproval()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);

        var result = evaluation.ApproveReport("chair-001");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("PendingApproval");
    }

    // ═══════════════════════════════════════════════════════════
    //  Rejection Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void RejectReport_Should_Set_Status_And_Reason()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1"));
        evaluation.MarkAllScoresSubmitted(UserId);
        evaluation.SubmitForApproval(UserId);

        var result = evaluation.RejectReport("chair-001", "Scores need review");

        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(TechnicalEvaluationStatus.Rejected);
        evaluation.RejectionReason.Should().Be("Scores need review");
    }

    [Fact]
    public void RejectReport_Should_Fail_Without_Reason()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1"));
        evaluation.MarkAllScoresSubmitted(UserId);
        evaluation.SubmitForApproval(UserId);

        var result = evaluation.RejectReport("chair-001", "");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason");
    }

    [Fact]
    public void ReopenForRescoring_Should_Return_To_InProgress()
    {
        var evaluation = CreateEvaluation();
        evaluation.Start(UserId);
        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1"));
        evaluation.MarkAllScoresSubmitted(UserId);
        evaluation.SubmitForApproval(UserId);
        evaluation.RejectReport("chair-001", "Needs review");

        var result = evaluation.ReopenForRescoring("chair-001");

        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(TechnicalEvaluationStatus.InProgress);
        evaluation.RejectionReason.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    //  Blind Evaluation Invariant
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BlindEvaluation_Should_Remain_Active_Until_Approval()
    {
        var evaluation = CreateEvaluation();

        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        evaluation.Start(UserId);
        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        evaluation.AddScore(TechnicalScore.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            "evaluator-1", 80m, 100m, null, "evaluator-1"));
        evaluation.MarkAllScoresSubmitted(UserId);
        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        evaluation.SubmitForApproval(UserId);
        evaluation.IsBlindEvaluationActive.Should().BeTrue();

        evaluation.ApproveReport("chair-001");
        evaluation.IsBlindEvaluationActive.Should().BeFalse();
    }
}
