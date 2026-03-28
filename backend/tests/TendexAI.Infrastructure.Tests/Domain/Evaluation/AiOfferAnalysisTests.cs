using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Evaluation;

/// <summary>
/// Unit tests for the AiOfferAnalysis domain entity.
/// Tests creation, criterion analysis management, and human review workflow.
/// Per RAG Guidelines: AI as Copilot — human retains final decision.
/// </summary>
public sealed class AiOfferAnalysisTests
{
    // ═══════════════════════════════════════════════════════════════
    //  Creation Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Create_Should_Initialize_All_Properties_Correctly()
    {
        // Arrange
        var evalId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var competitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var analysis = AiOfferAnalysis.Create(
            technicalEvaluationId: evalId,
            supplierOfferId: offerId,
            competitionId: competitionId,
            tenantId: tenantId,
            blindCode: "OFFER-A1",
            executiveSummary: "ملخص تنفيذي للعرض",
            strengthsAnalysis: "نقاط القوة",
            weaknessesAnalysis: "نقاط الضعف",
            risksAnalysis: "المخاطر المحتملة",
            complianceAssessment: "تقييم الامتثال",
            overallRecommendation: "التوصية العامة",
            overallComplianceScore: 85.5m,
            status: AiAnalysisStatus.Completed,
            aiModelUsed: "gpt-4o",
            aiProviderUsed: "OpenAI",
            analysisLatencyMs: 3500,
            createdBy: "system");

        // Assert
        analysis.Id.Should().NotBeEmpty();
        analysis.TechnicalEvaluationId.Should().Be(evalId);
        analysis.SupplierOfferId.Should().Be(offerId);
        analysis.CompetitionId.Should().Be(competitionId);
        analysis.TenantId.Should().Be(tenantId);
        analysis.BlindCode.Should().Be("OFFER-A1");
        analysis.ExecutiveSummary.Should().Be("ملخص تنفيذي للعرض");
        analysis.StrengthsAnalysis.Should().Be("نقاط القوة");
        analysis.WeaknessesAnalysis.Should().Be("نقاط الضعف");
        analysis.RisksAnalysis.Should().Be("المخاطر المحتملة");
        analysis.ComplianceAssessment.Should().Be("تقييم الامتثال");
        analysis.OverallRecommendation.Should().Be("التوصية العامة");
        analysis.OverallComplianceScore.Should().Be(85.5m);
        analysis.Status.Should().Be(AiAnalysisStatus.Completed);
        analysis.AiModelUsed.Should().Be("gpt-4o");
        analysis.AiProviderUsed.Should().Be("OpenAI");
        analysis.AnalysisLatencyMs.Should().Be(3500);
        analysis.IsHumanReviewed.Should().BeFalse();
        analysis.ReviewedBy.Should().BeNull();
        analysis.ReviewedAt.Should().BeNull();
        analysis.ReviewNotes.Should().BeNull();
        analysis.CriterionAnalyses.Should().BeEmpty();
        analysis.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Criterion Analysis Management Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void AddCriterionAnalysis_Should_Add_Successfully()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();
        var criterionId = Guid.NewGuid();

        var criterionAnalysis = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: analysis.Id,
            evaluationCriterionId: criterionId,
            criterionNameAr: "الخبرة الفنية",
            suggestedScore: 80m,
            maxScore: 100m,
            detailedJustification: "يمتلك المورد خبرة واسعة في المشاريع المماثلة",
            offerCitations: "صفحة 5: خبرة 10 سنوات في المشاريع الحكومية",
            bookletRequirementReference: "البند 5.1 - متطلبات الخبرة",
            complianceNotes: "متوافق مع متطلبات الخبرة المحددة",
            complianceLevel: AiCriterionComplianceLevel.FullyCompliant,
            createdBy: "system");

        // Act
        var result = analysis.AddCriterionAnalysis(criterionAnalysis);

        // Assert
        result.IsSuccess.Should().BeTrue();
        analysis.CriterionAnalyses.Should().HaveCount(1);
        analysis.CriterionAnalyses.First().CriterionNameAr.Should().Be("الخبرة الفنية");
    }

    [Fact]
    public void AddCriterionAnalysis_Should_Reject_Duplicate_Criterion()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();
        var criterionId = Guid.NewGuid();

        var first = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: analysis.Id,
            evaluationCriterionId: criterionId,
            criterionNameAr: "الخبرة الفنية",
            suggestedScore: 80m,
            maxScore: 100m,
            detailedJustification: "تبرير أول",
            offerCitations: "اقتباس أول",
            bookletRequirementReference: null,
            complianceNotes: "ملاحظات",
            complianceLevel: AiCriterionComplianceLevel.FullyCompliant,
            createdBy: "system");

        var duplicate = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: analysis.Id,
            evaluationCriterionId: criterionId,
            criterionNameAr: "الخبرة الفنية",
            suggestedScore: 90m,
            maxScore: 100m,
            detailedJustification: "تبرير ثاني",
            offerCitations: "اقتباس ثاني",
            bookletRequirementReference: null,
            complianceNotes: "ملاحظات",
            complianceLevel: AiCriterionComplianceLevel.FullyCompliant,
            createdBy: "system");

        analysis.AddCriterionAnalysis(first);

        // Act
        var result = analysis.AddCriterionAnalysis(duplicate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        analysis.CriterionAnalyses.Should().HaveCount(1);
    }

    [Fact]
    public void AddCriterionAnalysis_Should_Allow_Multiple_Different_Criteria()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();

        var criterion1 = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: analysis.Id,
            evaluationCriterionId: Guid.NewGuid(),
            criterionNameAr: "الخبرة الفنية",
            suggestedScore: 80m,
            maxScore: 100m,
            detailedJustification: "تبرير",
            offerCitations: "اقتباس",
            bookletRequirementReference: null,
            complianceNotes: "ملاحظات",
            complianceLevel: AiCriterionComplianceLevel.FullyCompliant,
            createdBy: "system");

        var criterion2 = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: analysis.Id,
            evaluationCriterionId: Guid.NewGuid(),
            criterionNameAr: "المنهجية",
            suggestedScore: 60m,
            maxScore: 100m,
            detailedJustification: "تبرير",
            offerCitations: "اقتباس",
            bookletRequirementReference: null,
            complianceNotes: "ملاحظات",
            complianceLevel: AiCriterionComplianceLevel.PartiallyCompliant,
            createdBy: "system");

        // Act
        var result1 = analysis.AddCriterionAnalysis(criterion1);
        var result2 = analysis.AddCriterionAnalysis(criterion2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        analysis.CriterionAnalyses.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Human Review Tests (AI as Copilot)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MarkAsReviewed_Should_Update_Review_Properties()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();

        // Act
        var result = analysis.MarkAsReviewed("user-123", "تمت المراجعة والموافقة على التحليل");

        // Assert
        result.IsSuccess.Should().BeTrue();
        analysis.IsHumanReviewed.Should().BeTrue();
        analysis.ReviewedBy.Should().Be("user-123");
        analysis.ReviewedAt.Should().NotBeNull();
        analysis.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        analysis.ReviewNotes.Should().Be("تمت المراجعة والموافقة على التحليل");
        analysis.Status.Should().Be(AiAnalysisStatus.Reviewed);
    }

    [Fact]
    public void MarkAsReviewed_Should_Fail_When_ReviewedBy_Is_Empty()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();

        // Act
        var result = analysis.MarkAsReviewed("", "ملاحظات");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Reviewer identity is required");
        analysis.IsHumanReviewed.Should().BeFalse();
    }

    [Fact]
    public void MarkAsReviewed_Should_Allow_Null_ReviewNotes()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();

        // Act
        var result = analysis.MarkAsReviewed("user-456", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        analysis.IsHumanReviewed.Should().BeTrue();
        analysis.ReviewNotes.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_Should_Update_Status_To_Failed()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();

        // Act
        analysis.MarkAsFailed();

        // Assert
        analysis.Status.Should().Be(AiAnalysisStatus.Failed);
    }

    // ═══════════════════════════════════════════════════════════════
    //  AiCriterionAnalysis Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void AiCriterionAnalysis_Create_Should_Initialize_Properties()
    {
        // Arrange & Act
        var analysisId = Guid.NewGuid();
        var criterionId = Guid.NewGuid();

        var criterionAnalysis = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: analysisId,
            evaluationCriterionId: criterionId,
            criterionNameAr: "فريق العمل",
            suggestedScore: 75m,
            maxScore: 100m,
            detailedJustification: "الفريق المقترح يتضمن خبرات متنوعة",
            offerCitations: "صفحة 12: فريق مكون من 5 مهندسين",
            bookletRequirementReference: "البند 6.3 - متطلبات فريق العمل",
            complianceNotes: "متوافق جزئياً - ينقص مدير مشروع معتمد",
            complianceLevel: AiCriterionComplianceLevel.PartiallyCompliant,
            createdBy: "system");

        // Assert
        criterionAnalysis.Id.Should().NotBeEmpty();
        criterionAnalysis.AiOfferAnalysisId.Should().Be(analysisId);
        criterionAnalysis.EvaluationCriterionId.Should().Be(criterionId);
        criterionAnalysis.CriterionNameAr.Should().Be("فريق العمل");
        criterionAnalysis.SuggestedScore.Should().Be(75m);
        criterionAnalysis.MaxScore.Should().Be(100m);
        criterionAnalysis.DetailedJustification.Should().Contain("خبرات متنوعة");
        criterionAnalysis.OfferCitations.Should().Contain("صفحة 12");
        criterionAnalysis.BookletRequirementReference.Should().Contain("البند 6.3");
        criterionAnalysis.ComplianceNotes.Should().Contain("ينقص مدير مشروع");
        criterionAnalysis.ComplianceLevel.Should().Be(AiCriterionComplianceLevel.PartiallyCompliant);
    }

    [Fact]
    public void AiCriterionAnalysis_GetScorePercentage_Should_Calculate_Correctly()
    {
        // Arrange
        var criterionAnalysis = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: Guid.NewGuid(),
            evaluationCriterionId: Guid.NewGuid(),
            criterionNameAr: "المنهجية",
            suggestedScore: 75m,
            maxScore: 100m,
            detailedJustification: "تبرير",
            offerCitations: "اقتباس",
            bookletRequirementReference: null,
            complianceNotes: "ملاحظات",
            complianceLevel: AiCriterionComplianceLevel.PartiallyCompliant,
            createdBy: "system");

        // Act
        var percentage = criterionAnalysis.GetScorePercentage();

        // Assert
        percentage.Should().Be(75m);
    }

    [Fact]
    public void AiCriterionAnalysis_GetScorePercentage_Should_Return_Zero_When_MaxScore_Is_Zero()
    {
        // Arrange
        var criterionAnalysis = AiCriterionAnalysis.Create(
            aiOfferAnalysisId: Guid.NewGuid(),
            evaluationCriterionId: Guid.NewGuid(),
            criterionNameAr: "معيار",
            suggestedScore: 50m,
            maxScore: 0m,
            detailedJustification: "تبرير",
            offerCitations: "اقتباس",
            bookletRequirementReference: null,
            complianceNotes: "ملاحظات",
            complianceLevel: AiCriterionComplianceLevel.NotApplicable,
            createdBy: "system");

        // Act
        var percentage = criterionAnalysis.GetScorePercentage();

        // Assert
        percentage.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private static AiOfferAnalysis CreateSampleAnalysis()
    {
        return AiOfferAnalysis.Create(
            technicalEvaluationId: Guid.NewGuid(),
            supplierOfferId: Guid.NewGuid(),
            competitionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            blindCode: "OFFER-A1",
            executiveSummary: "ملخص تنفيذي",
            strengthsAnalysis: "نقاط القوة",
            weaknessesAnalysis: "نقاط الضعف",
            risksAnalysis: "المخاطر",
            complianceAssessment: "تقييم الامتثال",
            overallRecommendation: "التوصية",
            overallComplianceScore: 80m,
            status: AiAnalysisStatus.Completed,
            aiModelUsed: "gpt-4o",
            aiProviderUsed: "OpenAI",
            analysisLatencyMs: 2500,
            createdBy: "system");
    }
}
