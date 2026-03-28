using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.AI;

namespace TendexAI.Infrastructure.Tests.AI;

/// <summary>
/// Unit tests for AiSpecificationDraftingService.
/// Tests section draft generation, refinement, booklet structure generation,
/// RAG context integration, grounding/citation enforcement, and error handling.
/// </summary>
public sealed class AiSpecificationDraftingServiceTests
{
    private readonly Mock<IAiGateway> _gatewayMock;
    private readonly Mock<IContextRetrievalService> _contextRetrievalMock;
    private readonly Mock<ILogger<AiSpecificationDraftingService>> _loggerMock;
    private readonly AiSpecificationDraftingService _service;

    public AiSpecificationDraftingServiceTests()
    {
        _gatewayMock = new Mock<IAiGateway>();
        _contextRetrievalMock = new Mock<IContextRetrievalService>();
        _loggerMock = new Mock<ILogger<AiSpecificationDraftingService>>();

        _service = new AiSpecificationDraftingService(
            _gatewayMock.Object,
            _contextRetrievalMock.Object,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateSectionDraftAsync Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Return_Success_With_Valid_Response()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidSectionDraftJson());

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ContentHtml.Should().NotBeNullOrEmpty();
        result.Value.ContentPlainText.Should().NotBeNullOrEmpty();
        result.Value.Citations.Should().NotBeEmpty();
        result.Value.ProviderName.Should().NotBeNullOrEmpty();
        result.Value.ModelName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Include_Citations_From_Response()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidSectionDraftJson());

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Citations.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Value.Citations[0].DocumentName.Should().NotBeNullOrEmpty();
        result.Value.Citations[0].QuotedText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Include_Regulatory_References()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidSectionDraftJson());

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RegulatoryReferences.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Value.RegulatoryReferences[0].RegulationNameAr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Return_Grounding_Score()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidSectionDraftJson());

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.GroundingConfidenceScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Return_Failure_When_Gateway_Fails()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        SetupAiGatewayFailure("Rate limit exceeded");

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("فشل توليد المسودة");
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Proceed_Without_RAG_Context()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContextFailure();
        SetupAiGatewaySuccess(CreateValidSectionDraftJson());

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Handle_Invalid_Json_Response()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        SetupAiGatewaySuccess("This is not valid JSON");

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("فشل في تحليل استجابة JSON");
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Handle_Markdown_Wrapped_Json()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        var wrappedJson = "```json\n" + CreateValidSectionDraftJson() + "\n```";
        SetupAiGatewaySuccess(wrappedJson);

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Use_Low_Temperature()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidSectionDraftJson());

        // Act
        await _service.GenerateSectionDraftAsync(request);

        // Assert - verify temperature is set low for formal Arabic consistency
        _gatewayMock.Verify(g => g.GenerateCompletionAsync(
            It.Is<AiCompletionRequest>(r => r.TemperatureOverride <= 0.3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Enrich_Citations_From_RAG_Chunks()
    {
        // Arrange
        var request = CreateSectionDraftRequest();
        SetupRagContextWithChunks();
        SetupAiGatewaySuccess(CreateMinimalSectionDraftJson());

        // Act
        var result = await _service.GenerateSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should have citations enriched from RAG chunks
        result.Value!.Citations.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RefineSectionDraftAsync Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefineSectionDraftAsync_Should_Return_Success_With_Valid_Response()
    {
        // Arrange
        var request = CreateRefineRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidSectionDraftJson());

        // Act
        var result = await _service.RefineSectionDraftAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RefineSectionDraftAsync_Should_Return_Failure_When_Gateway_Fails()
    {
        // Arrange
        var request = CreateRefineRequest();
        SetupRagContext();
        SetupAiGatewayFailure("Service unavailable");

        // Act
        var result = await _service.RefineSectionDraftAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("فشل تحسين المسودة");
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateBookletStructureAsync Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateBookletStructureAsync_Should_Return_Success_With_Sections()
    {
        // Arrange
        var request = CreateBookletStructureRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBookletStructureJson());

        // Act
        var result = await _service.GenerateBookletStructureAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Sections.Should().NotBeEmpty();
        result.Value.StructureSummaryAr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateBookletStructureAsync_Should_Include_Mandatory_Sections()
    {
        // Arrange
        var request = CreateBookletStructureRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBookletStructureJson());

        // Act
        var result = await _service.GenerateBookletStructureAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Sections.Should().Contain(s => s.IsMandatory);
    }

    [Fact]
    public async Task GenerateBookletStructureAsync_Should_Return_Failure_When_Gateway_Fails()
    {
        // Arrange
        var request = CreateBookletStructureRequest();
        SetupRagContext();
        SetupAiGatewayFailure("Timeout");

        // Act
        var result = await _service.GenerateBookletStructureAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("فشل توليد هيكل الكراسة");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Null/Edge Case Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateSectionDraftAsync_Should_Throw_On_Null_Request()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GenerateSectionDraftAsync(null!));
    }

    [Fact]
    public async Task RefineSectionDraftAsync_Should_Throw_On_Null_Request()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.RefineSectionDraftAsync(null!));
    }

    [Fact]
    public async Task GenerateBookletStructureAsync_Should_Throw_On_Null_Request()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GenerateBookletStructureAsync(null!));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private static AiSpecificationDraftRequest CreateSectionDraftRequest()
    {
        return new AiSpecificationDraftRequest
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع تطوير نظام إلكتروني",
            ProjectDescriptionAr = "تطوير نظام إلكتروني متكامل لإدارة الموارد البشرية",
            ProjectType = "تقنية معلومات",
            EstimatedBudget = 500000,
            SectionType = "TechnicalSpecifications",
            SectionTitleAr = "المواصفات الفنية",
            CollectionName = "rfp_knowledge_base"
        };
    }

    private static AiSpecificationRefineRequest CreateRefineRequest()
    {
        return new AiSpecificationRefineRequest
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع تطوير نظام إلكتروني",
            SectionTitleAr = "المواصفات الفنية",
            CurrentContentHtml = "<h3>المواصفات الفنية</h3><p>محتوى المسودة الحالية</p>",
            UserFeedbackAr = "يرجى إضافة متطلبات الأمان السيبراني",
            CollectionName = "rfp_knowledge_base"
        };
    }

    private static AiBookletStructureRequest CreateBookletStructureRequest()
    {
        return new AiBookletStructureRequest
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع تطوير نظام إلكتروني",
            ProjectDescriptionAr = "تطوير نظام إلكتروني متكامل لإدارة الموارد البشرية",
            ProjectType = "تقنية معلومات",
            EstimatedBudget = 500000,
            CollectionName = "rfp_knowledge_base"
        };
    }

    private void SetupRagContext()
    {
        _contextRetrievalMock
            .Setup(c => c.RetrieveContextAsync(
                It.IsAny<ContextRetrievalRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContextRetrievalResult
            {
                IsSuccess = true,
                FormattedContext = "سياق مرجعي من قاعدة المعرفة",
                Chunks = new List<RetrievedChunk>(),
                TotalCandidates = 5,
                RetrievalTimeMs = 100
            });
    }

    private void SetupRagContextWithChunks()
    {
        _contextRetrievalMock
            .Setup(c => c.RetrieveContextAsync(
                It.IsAny<ContextRetrievalRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContextRetrievalResult
            {
                IsSuccess = true,
                FormattedContext = "سياق مرجعي من قاعدة المعرفة",
                Chunks = new List<RetrievedChunk>
                {
                    new()
                    {
                        DocumentName = "نظام المنافسات والمشتريات الحكومية",
                        DocumentId = Guid.NewGuid(),
                        SectionName = "المادة 23",
                        PageNumbers = "15",
                        Text = "يجب أن تتضمن كراسة الشروط والمواصفات وصفاً دقيقاً للأعمال المطلوبة",
                        ContextualHeader = "المادة 23 - نظام المنافسات",
                        Score = 0.85f
                    }
                },
                TotalCandidates = 10,
                RetrievalTimeMs = 150
            });
    }

    private void SetupRagContextFailure()
    {
        _contextRetrievalMock
            .Setup(c => c.RetrieveContextAsync(
                It.IsAny<ContextRetrievalRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContextRetrievalResult
            {
                IsSuccess = false,
                ErrorMessage = "Qdrant connection failed",
                Chunks = new List<RetrievedChunk>(),
                TotalCandidates = 0,
                RetrievalTimeMs = 0
            });
    }

    private void SetupAiGatewaySuccess(string content)
    {
        _gatewayMock
            .Setup(g => g.GenerateCompletionAsync(
                It.IsAny<AiCompletionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Success(
                content, AiProvider.OpenAI, "gpt-4o", 500, 1000, 2000));
    }

    private void SetupAiGatewayFailure(string errorMessage)
    {
        _gatewayMock
            .Setup(g => g.GenerateCompletionAsync(
                It.IsAny<AiCompletionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Failure(
                errorMessage, AiProvider.OpenAI, "gpt-4o"));
    }

    private static string CreateValidSectionDraftJson()
    {
        return """
            {
              "contentHtml": "<h3>المواصفات الفنية</h3><p>يشمل نطاق العمل تطوير نظام إلكتروني متكامل وفقاً للمتطلبات المحددة.</p>",
              "contentPlainText": "المواصفات الفنية\nيشمل نطاق العمل تطوير نظام إلكتروني متكامل وفقاً للمتطلبات المحددة.",
              "citations": [
                {
                  "documentName": "نظام المنافسات والمشتريات الحكومية",
                  "sectionReference": "المادة 23",
                  "pageNumbers": "15",
                  "quotedText": "يجب أن تتضمن كراسة الشروط والمواصفات وصفاً دقيقاً للأعمال المطلوبة",
                  "usageContext": "تأسيس متطلب تحديد نطاق العمل"
                }
              ],
              "regulatoryReferences": [
                {
                  "regulationNameAr": "نظام المنافسات والمشتريات الحكومية",
                  "articleNumber": "المادة 23",
                  "requirementSummaryAr": "وجوب تضمين الكراسة وصفاً دقيقاً للأعمال المطلوبة"
                }
              ],
              "groundingConfidenceScore": 85.0,
              "warnings": []
            }
            """;
    }

    private static string CreateMinimalSectionDraftJson()
    {
        return """
            {
              "contentHtml": "<p>محتوى القسم</p>",
              "contentPlainText": "محتوى القسم",
              "citations": [],
              "regulatoryReferences": [],
              "groundingConfidenceScore": 50.0,
              "warnings": ["لم يتم العثور على مراجع كافية"]
            }
            """;
    }

    private static string CreateValidBookletStructureJson()
    {
        return """
            {
              "sections": [
                {
                  "titleAr": "معلومات عامة عن المنافسة",
                  "titleEn": "General Competition Information",
                  "sectionType": "GeneralInformation",
                  "isMandatory": true,
                  "descriptionAr": "يتضمن هذا القسم المعلومات الأساسية عن المنافسة",
                  "sortOrder": 1
                },
                {
                  "titleAr": "المواصفات الفنية",
                  "titleEn": "Technical Specifications",
                  "sectionType": "TechnicalSpecifications",
                  "isMandatory": true,
                  "descriptionAr": "يتضمن المتطلبات الفنية التفصيلية",
                  "sortOrder": 2
                },
                {
                  "titleAr": "جدول الكميات",
                  "titleEn": "Bill of Quantities",
                  "sectionType": "BillOfQuantities",
                  "isMandatory": true,
                  "descriptionAr": "جدول الكميات والأسعار",
                  "sortOrder": 3
                }
              ],
              "structureSummaryAr": "هيكل كراسة مقترح يتضمن 3 أقسام إلزامية",
              "citations": [
                {
                  "documentName": "الدليل الاسترشادي",
                  "sectionReference": "الفصل 2",
                  "quotedText": "يجب أن تتضمن الكراسة الأقسام الإلزامية",
                  "usageContext": "تحديد الأقسام الإلزامية"
                }
              ]
            }
            """;
    }
}
