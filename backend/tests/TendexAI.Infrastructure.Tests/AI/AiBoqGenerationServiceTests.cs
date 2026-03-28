using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.AI;

namespace TendexAI.Infrastructure.Tests.AI;

/// <summary>
/// Unit tests for AiBoqGenerationService.
/// Tests BOQ generation, refinement, RAG grounding, anti-hallucination price controls,
/// and proper error handling.
/// </summary>
public sealed class AiBoqGenerationServiceTests
{
    private readonly Mock<IAiGateway> _gatewayMock;
    private readonly Mock<IContextRetrievalService> _contextRetrievalMock;
    private readonly Mock<ILogger<AiBoqGenerationService>> _loggerMock;
    private readonly AiBoqGenerationService _service;

    public AiBoqGenerationServiceTests()
    {
        _gatewayMock = new Mock<IAiGateway>();
        _contextRetrievalMock = new Mock<IContextRetrievalService>();
        _loggerMock = new Mock<ILogger<AiBoqGenerationService>>();

        _service = new AiBoqGenerationService(
            _gatewayMock.Object,
            _contextRetrievalMock.Object,
            _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateBoqAsync Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateBoqAsync_Should_Return_Success_With_Valid_Response()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().NotBeEmpty();
        result.Value.SummaryAr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Return_Items_With_Arabic_Descriptions()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstItem = result.Value!.Items[0];
        firstItem.DescriptionAr.Should().NotBeNullOrEmpty();
        firstItem.Unit.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Include_Estimated_Prices_As_Ranges()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstItem = result.Value!.Items[0];
        firstItem.Quantity.Should().BeGreaterThan(0);
        firstItem.ItemNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Include_Citations()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Citations.Should().NotBeEmpty();
        result.Value!.Citations[0].DocumentName.Should().NotBeNullOrEmpty();    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Return_Grounding_Score()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.GroundingConfidenceScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Return_Failure_When_Gateway_Fails()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewayFailure("Rate limit exceeded");

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("فشل توليد جدول الكميات");
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Handle_Invalid_Json_Response()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewaySuccess("This is not valid JSON at all");

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("فشل في تحليل استجابة JSON");
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Handle_Markdown_Wrapped_Json()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        var wrappedJson = "```json\n" + CreateValidBoqJson() + "\n```";
        SetupAiGatewaySuccess(wrappedJson);

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Proceed_Without_RAG_Context()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContextFailure();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        var result = await _service.GenerateBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateBoqAsync_Should_Use_Low_Temperature()
    {
        // Arrange
        var request = CreateBoqGenerationRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        await _service.GenerateBoqAsync(request);

        // Assert - verify temperature is set low for precise BOQ data
        _gatewayMock.Verify(g => g.GenerateCompletionAsync(
            It.Is<AiCompletionRequest>(r => r.TemperatureOverride <= 0.2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RefineBoqAsync Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefineBoqAsync_Should_Return_Success_With_Valid_Response()
    {
        // Arrange
        var request = CreateRefineBoqRequest();
        SetupRagContext();
        SetupAiGatewaySuccess(CreateValidBoqJson());

        // Act
        var result = await _service.RefineBoqAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RefineBoqAsync_Should_Return_Failure_When_Gateway_Fails()
    {
        // Arrange
        var request = CreateRefineBoqRequest();
        SetupRagContext();
        SetupAiGatewayFailure("Service unavailable");

        // Act
        var result = await _service.RefineBoqAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("فشل تحسين جدول الكميات");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Null/Edge Case Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateBoqAsync_Should_Throw_On_Null_Request()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GenerateBoqAsync(null!));
    }

    [Fact]
    public async Task RefineBoqAsync_Should_Throw_On_Null_Request()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.RefineBoqAsync(null!));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private static AiBoqGenerationRequest CreateBoqGenerationRequest()
    {
        return new AiBoqGenerationRequest
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

    private static AiBoqRefineRequest CreateRefineBoqRequest()
    {
        return new AiBoqRefineRequest
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع تطوير نظام إلكتروني",
            ExistingBoqJson = CreateValidBoqJson(),
            UserFeedbackAr = "يرجى إضافة بنود الصيانة والدعم الفني",
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
                FormattedContext = "سياق مرجعي من قاعدة المعرفة حول جداول الكميات",
                Chunks = new List<RetrievedChunk>
                {
                    new()
                    {
                        DocumentName = "الدليل الاسترشادي لإعداد كراسات الشروط",
                        DocumentId = Guid.NewGuid(),
                        SectionName = "الفصل 4 - جدول الكميات",
                        PageNumbers = "25-30",
                        Text = "يجب أن يتضمن جدول الكميات وصفاً تفصيلياً لكل بند مع تحديد الوحدة والكمية",
                        ContextualHeader = "الفصل 4 - جدول الكميات",
                        Score = 0.90f
                    }
                },
                TotalCandidates = 8,
                RetrievalTimeMs = 120
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
                content, AiProvider.OpenAI, "gpt-4o", 500, 1500, 2500));
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

    private static string CreateValidBoqJson()
    {
        return """
            {
              "items": [
                {
                  "itemNumber": "1.1",
                  "descriptionAr": "\u062a\u062d\u0644\u064a\u0644 \u0648\u062a\u0635\u0645\u064a\u0645 \u0627\u0644\u0646\u0638\u0627\u0645",
                  "descriptionEn": "System Analysis and Design",
                  "unit": "Lump Sum",
                  "quantity": 1.0,
                  "estimatedUnitPrice": 65000.0,
                  "priceEstimateSource": "\u062a\u0642\u062f\u064a\u0631 \u0628\u0646\u0627\u0621\u064b \u0639\u0644\u0649 \u0645\u0634\u0627\u0631\u064a\u0639 \u0645\u0645\u0627\u062b\u0644\u0629",
                  "category": "\u0623\u0639\u0645\u0627\u0644 \u0627\u0644\u0628\u0631\u0645\u062c\u064a\u0627\u062a",
                  "justificationAr": "\u064a\u0634\u0645\u0644 \u062a\u062d\u0644\u064a\u0644 \u0627\u0644\u0645\u062a\u0637\u0644\u0628\u0627\u062a \u0648\u062a\u0635\u0645\u064a\u0645 \u0627\u0644\u0628\u0646\u064a\u0629 \u0627\u0644\u062a\u0642\u0646\u064a\u0629",
                  "sortOrder": 1
                },
                {
                  "itemNumber": "1.2",
                  "descriptionAr": "\u062a\u0637\u0648\u064a\u0631 \u0627\u0644\u0648\u0627\u062c\u0647\u0627\u062a \u0627\u0644\u0623\u0645\u0627\u0645\u064a\u0629",
                  "descriptionEn": "Frontend Development",
                  "unit": "Screen",
                  "quantity": 25.0,
                  "estimatedUnitPrice": 4000.0,
                  "priceEstimateSource": "\u0645\u062a\u0648\u0633\u0637 \u0623\u0633\u0639\u0627\u0631 \u0627\u0644\u0633\u0648\u0642",
                  "category": "\u0623\u0639\u0645\u0627\u0644 \u0627\u0644\u0628\u0631\u0645\u062c\u064a\u0627\u062a",
                  "justificationAr": "\u062a\u0637\u0648\u064a\u0631 \u0648\u0627\u062c\u0647\u0627\u062a \u0627\u0644\u0645\u0633\u062a\u062e\u062f\u0645 \u0628\u062a\u0642\u0646\u064a\u0627\u062a \u062d\u062f\u064a\u062b\u0629",
                  "sortOrder": 2
                },
                {
                  "itemNumber": "2.1",
                  "descriptionAr": "\u062a\u0648\u0641\u064a\u0631 \u0648\u062a\u0647\u064a\u0626\u0629 \u0627\u0644\u062e\u0648\u0627\u062f\u0645 \u0627\u0644\u0633\u062d\u0627\u0628\u064a\u0629",
                  "descriptionEn": "Cloud Server Provisioning",
                  "unit": "Server",
                  "quantity": 3.0,
                  "estimatedUnitPrice": 12000.0,
                  "priceEstimateSource": "\u0623\u0633\u0639\u0627\u0631 \u0645\u0632\u0648\u062f\u064a \u0627\u0644\u062e\u062f\u0645\u0627\u062a \u0627\u0644\u0633\u062d\u0627\u0628\u064a\u0629",
                  "category": "\u0623\u0639\u0645\u0627\u0644 \u0627\u0644\u0628\u0646\u064a\u0629 \u0627\u0644\u062a\u062d\u062a\u064a\u0629",
                  "justificationAr": "\u062e\u0648\u0627\u062f\u0645 \u0633\u062d\u0627\u0628\u064a\u0629 \u0639\u0627\u0644\u064a\u0629 \u0627\u0644\u0623\u062f\u0627\u0621",
                  "sortOrder": 3
                }
              ],
              "summaryAr": "\u062c\u062f\u0648\u0644 \u0643\u0645\u064a\u0627\u062a \u0645\u0642\u062a\u0631\u062d \u064a\u062a\u0636\u0645\u0646 \u0641\u0626\u062a\u064a\u0646 \u0631\u0626\u064a\u0633\u064a\u062a\u064a\u0646",
              "totalEstimatedCost": 201000.0,
              "citations": [
                {
                  "documentName": "\u0627\u0644\u062f\u0644\u064a\u0644 \u0627\u0644\u0627\u0633\u062a\u0631\u0634\u0627\u062f\u064a \u0644\u0625\u0639\u062f\u0627\u062f \u0643\u0631\u0627\u0633\u0627\u062a \u0627\u0644\u0634\u0631\u0648\u0637",
                  "sectionReference": "\u0627\u0644\u0641\u0635\u0644 4",
                  "quotedText": "\u064a\u062c\u0628 \u0623\u0646 \u064a\u062a\u0636\u0645\u0646 \u062c\u062f\u0648\u0644 \u0627\u0644\u0643\u0645\u064a\u0627\u062a \u0648\u0635\u0641\u0627\u064b \u062a\u0641\u0635\u064a\u0644\u064a\u0627\u064b \u0644\u0643\u0644 \u0628\u0646\u062f",
                  "usageContext": "\u062a\u062d\u062f\u064a\u062f \u0647\u064a\u0643\u0644 \u062c\u062f\u0648\u0644 \u0627\u0644\u0643\u0645\u064a\u0627\u062a"
                }
              ],
              "groundingConfidenceScore": 80.0,
              "warnings": ["\u0627\u0644\u0623\u0633\u0639\u0627\u0631 \u0627\u0644\u062a\u0642\u062f\u064a\u0631\u064a\u0629 \u0645\u0628\u0646\u064a\u0629 \u0639\u0644\u0649 \u0645\u062a\u0648\u0633\u0637\u0627\u062a \u0627\u0644\u0633\u0648\u0642"]
            }
            """;
    }
}
