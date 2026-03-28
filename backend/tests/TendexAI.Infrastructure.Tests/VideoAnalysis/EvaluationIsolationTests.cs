using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Services;
using TendexAI.Infrastructure.AI.VideoAnalysis;

namespace TendexAI.Infrastructure.Tests.VideoAnalysis;

/// <summary>
/// Tests to verify that the Video Integrity Analysis feature is completely
/// isolated from the core evaluation pipeline (Technical and Financial scoring).
/// 
/// These tests ensure TASK-405 requirement: "التأكد من عدم التأثير سلباً على منطق التقييم الأساسي"
/// </summary>
public sealed class EvaluationIsolationTests
{
    [Fact]
    public void VideoIntegrityService_Should_Only_Depend_On_IAiGateway_And_ILogger()
    {
        // Verify constructor dependencies are minimal and don't include evaluation services
        var constructors = typeof(VideoIntegrityService).GetConstructors();
        constructors.Should().HaveCount(1);

        var parameters = constructors[0].GetParameters();
        parameters.Should().HaveCount(2);

        var parameterTypeNames = parameters.Select(p => p.ParameterType.Name).ToList();
        parameterTypeNames.Should().Contain("IAiGateway");
        parameterTypeNames.Should().Contain(n => n.StartsWith("ILogger"));

        // Must NOT contain any evaluation-related dependencies
        parameterTypeNames.Should().NotContain(n =>
            n.Contains("TechnicalScoring") ||
            n.Contains("FinancialScoring") ||
            n.Contains("TechnicalEvaluation") ||
            n.Contains("FinancialEvaluation") ||
            n.Contains("SupplierOffer"));
    }

    [Fact]
    public void VideoIntegrityAnalysis_Entity_Should_Not_Navigate_To_Evaluation_Entities()
    {
        // Verify the entity doesn't have navigation properties to evaluation entities
        var entityType = typeof(VideoIntegrityAnalysis);
        var properties = entityType.GetProperties();

        var propertyNames = properties.Select(p => p.Name).ToList();

        propertyNames.Should().NotContain("TechnicalEvaluation");
        propertyNames.Should().NotContain("TechnicalScore");
        propertyNames.Should().NotContain("TechnicalScores");
        propertyNames.Should().NotContain("AiTechnicalScore");
        propertyNames.Should().NotContain("FinancialEvaluation");
    }

    [Fact]
    public void VideoIntegrityAnalysis_Should_Use_Separate_Table()
    {
        // The entity should be stored in its own table, not mixed with evaluation tables
        var entityType = typeof(VideoIntegrityAnalysis);

        // Verify it doesn't inherit from any evaluation entity
        entityType.BaseType.Should().NotBe<TechnicalEvaluation>();
        entityType.BaseType.Should().NotBe<TechnicalScore>();
        entityType.BaseType.Should().NotBe<AiTechnicalScore>();
    }

    [Fact]
    public void TechnicalScoringService_Should_Not_Reference_VideoIntegrity()
    {
        // Verify the existing TechnicalScoringService doesn't depend on video integrity
        var scoringServiceType = typeof(TechnicalScoringService);
        var methods = scoringServiceType.GetMethods();

        var methodNames = methods.Select(m => m.Name).ToList();
        methodNames.Should().NotContain(n =>
            n.Contains("Video") ||
            n.Contains("Integrity") ||
            n.Contains("Tamper"));

        // Check method parameters don't reference video types
        foreach (var method in methods)
        {
            var paramTypes = method.GetParameters().Select(p => p.ParameterType.Name);
            paramTypes.Should().NotContain(n =>
                n.Contains("VideoIntegrity") ||
                n.Contains("VideoAnalysis") ||
                n.Contains("TamperDetection"));
        }
    }

    [Fact]
    public void VideoIntegrityAnalysis_Should_Reference_Competition_By_Id_Only()
    {
        // The entity should only store CompetitionId as a foreign key,
        // not have a navigation property to Competition
        var entityType = typeof(VideoIntegrityAnalysis);
        var properties = entityType.GetProperties();

        // Should have CompetitionId
        properties.Should().Contain(p => p.Name == "CompetitionId");

        // Should NOT have Competition navigation property
        var propertyNames = properties.Select(p => p.Name).ToList();
        propertyNames.Should().NotContain("Competition");
    }
}
