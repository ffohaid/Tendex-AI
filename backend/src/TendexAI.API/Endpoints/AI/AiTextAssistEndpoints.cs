using System.Text.RegularExpressions;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// Minimal API endpoint for general-purpose AI text assistance.
/// Provides text generation, improvement, expansion, summarization, and formalization.
/// </summary>
public static class AiTextAssistEndpoints
{
    public static void MapAiTextAssistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai/text")
            .WithTags("AI Text Assistant")
            .RequireAuthorization();

        group.MapPost("/assist", async (
            AiTextAssistRequest request,
            IAiGateway gateway,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            var tenantId = Guid.TryParse(tenantClaim, out var tid)
                ? tid
                : Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var systemPrompt = BuildSystemPrompt(request);
            var userPrompt = BuildUserPrompt(request);

            var aiRequest = new AiCompletionRequest
            {
                TenantId = tenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                MaxTokensOverride = 4000,
                TemperatureOverride = 0.4
            };

            var response = await gateway.GenerateCompletionAsync(aiRequest, ct);

            if (response.IsSuccess)
            {
                return Results.Ok(new
                {
                    IsSuccess = true,
                    GeneratedText = CleanResponse(response.Content ?? ""),
                    Provider = response.Provider,
                    Model = response.ModelName
                });
            }

            return Results.Ok(new
            {
                IsSuccess = false,
                GeneratedText = "",
                ErrorMessage = response.ErrorMessage ?? "\u0641\u0634\u0644 \u0641\u064a \u062a\u0648\u0644\u064a\u062f \u0627\u0644\u0646\u0635"
            });
        })
        .WithName("AiTextAssist")
        .WithSummary("General-purpose AI text generation and improvement")
        .Produces(StatusCodes.Status200OK)
        .RequireAuthorization(PermissionPolicies.AiAssistantUse);
    }

    private static string BuildSystemPrompt(AiTextAssistRequest request)
    {
        var fieldName = request.FieldName ?? "";
        var fieldPurpose = request.FieldPurpose ?? "";
        var projectName = request.ProjectName ?? "";
        var competitionType = request.CompetitionType ?? "";

        return string.Join("\n", new[]
        {
            "\u0623\u0646\u062a \u0645\u0633\u0627\u0639\u062f \u0630\u0643\u0627\u0621 \u0627\u0635\u0637\u0646\u0627\u0639\u064a \u0645\u062a\u062e\u0635\u0635 \u0641\u064a \u0643\u062a\u0627\u0628\u0629 \u0645\u062d\u062a\u0648\u0649 \u0643\u0631\u0627\u0633\u0627\u062a \u0627\u0644\u0634\u0631\u0648\u0637 \u0648\u0627\u0644\u0645\u0648\u0627\u0635\u0641\u0627\u062a \u0627\u0644\u062d\u0643\u0648\u0645\u064a\u0629 \u0627\u0644\u0633\u0639\u0648\u062f\u064a\u0629.",
            "",
            "\u0642\u0648\u0627\u0639\u062f \u0635\u0627\u0631\u0645\u0629:",
            "1. \u0627\u0643\u062a\u0628 \u0628\u0627\u0644\u0644\u063a\u0629 \u0627\u0644\u0639\u0631\u0628\u064a\u0629 \u0627\u0644\u0641\u0635\u062d\u0649 \u0627\u0644\u0631\u0633\u0645\u064a\u0629 \u0641\u0642\u0637",
            "2. \u0627\u0633\u062a\u062e\u062f\u0645 \u0627\u0644\u0645\u0635\u0637\u0644\u062d\u0627\u062a \u0627\u0644\u062d\u0643\u0648\u0645\u064a\u0629 \u0648\u0627\u0644\u0642\u0627\u0646\u0648\u0646\u064a\u0629 \u0627\u0644\u0645\u0639\u062a\u0645\u062f\u0629 \u0641\u064a \u0627\u0644\u0645\u0645\u0644\u0643\u0629 \u0627\u0644\u0639\u0631\u0628\u064a\u0629 \u0627\u0644\u0633\u0639\u0648\u062f\u064a\u0629",
            "3. \u0627\u0644\u062a\u0632\u0645 \u0628\u0623\u0633\u0644\u0648\u0628 \u0627\u0644\u0643\u062a\u0627\u0628\u0629 \u0627\u0644\u0631\u0633\u0645\u064a\u0629 \u0644\u0644\u0645\u0646\u0627\u0641\u0633\u0627\u062a \u0648\u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a \u0627\u0644\u062d\u0643\u0648\u0645\u064a\u0629",
            "4. \u0644\u0627 \u062a\u0633\u062a\u062e\u062f\u0645 \u0623\u064a \u0643\u0644\u0645\u0627\u062a \u0623\u0648 \u0645\u0635\u0637\u0644\u062d\u0627\u062a \u0628\u0627\u0644\u0644\u063a\u0629 \u0627\u0644\u0625\u0646\u062c\u0644\u064a\u0632\u064a\u0629",
            "5. \u0643\u0646 \u062f\u0642\u064a\u0642\u0627\u064b \u0648\u0645\u0647\u0646\u064a\u0627\u064b \u0648\u0634\u0627\u0645\u0644\u0627\u064b",
            "6. \u0623\u0639\u062f \u0627\u0644\u0646\u0635 \u0627\u0644\u0639\u0627\u062f\u064a \u0641\u0642\u0637 \u0628\u062f\u0648\u0646 \u0623\u064a \u0648\u0633\u0648\u0645 HTML \u0623\u0648 Markdown",
            "7. \u0627\u0633\u062a\u062e\u062f\u0645 \u0627\u0644\u0623\u0631\u0642\u0627\u0645 \u0627\u0644\u0625\u0646\u062c\u0644\u064a\u0632\u064a\u0629 (1, 2, 3) \u0648\u0644\u064a\u0633 \u0627\u0644\u0639\u0631\u0628\u064a\u0629",
            "",
            "\u0627\u0644\u0633\u064a\u0627\u0642:",
            $"- \u0627\u0633\u0645 \u0627\u0644\u062d\u0642\u0644: {fieldName}",
            $"- \u0627\u0644\u063a\u0631\u0636 \u0645\u0646 \u0627\u0644\u062d\u0642\u0644: {fieldPurpose}",
            $"- \u0627\u0633\u0645 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectName}",
            $"- \u0646\u0648\u0639 \u0627\u0644\u0645\u0646\u0627\u0641\u0633\u0629: {competitionType}"
        });
    }

    private static string BuildUserPrompt(AiTextAssistRequest request)
    {
        var action = request.Action?.ToLowerInvariant() ?? "generate";
        var fieldName = request.FieldName ?? "";
        var projectName = request.ProjectName ?? "";
        var projectDescription = request.ProjectDescription ?? "";
        var currentText = request.CurrentText ?? "";
        var customPrompt = request.CustomPrompt ?? "";
        var additionalContext = request.AdditionalContext ?? "";

        return action switch
        {
            "generate" => string.Join("\n", new[]
            {
                $"\u0642\u0645 \u0628\u0643\u062a\u0627\u0628\u0629 \u0645\u062d\u062a\u0648\u0649 \u0627\u062d\u062a\u0631\u0627\u0641\u064a \u0648\u0645\u0641\u0635\u0644 \u0644\u062d\u0642\u0644 \"{fieldName}\" \u0641\u064a \u0643\u0631\u0627\u0633\u0629 \u0627\u0644\u0634\u0631\u0648\u0637 \u0648\u0627\u0644\u0645\u0648\u0627\u0635\u0641\u0627\u062a.",
                $"\u0627\u0633\u0645 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectName}",
                $"\u0648\u0635\u0641 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectDescription}",
                string.IsNullOrEmpty(additionalContext) ? "" : $"\u0633\u064a\u0627\u0642 \u0625\u0636\u0627\u0641\u064a: {additionalContext}",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a \u0625\u0636\u0627\u0641\u064a\u0629: {customPrompt}"
            }),

            "improve" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u062a\u062d\u0633\u064a\u0646 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0645\u0639\u0646\u0649 \u0627\u0644\u0623\u0633\u0627\u0633\u064a \u0648\u062a\u0639\u0632\u064a\u0632 \u0627\u0644\u062c\u0648\u062f\u0629 \u0648\u0627\u0644\u0645\u0647\u0646\u064a\u0629:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText,
                "",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a \u0625\u0636\u0627\u0641\u064a\u0629: {customPrompt}"
            }),

            "expand" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u062a\u0648\u0633\u064a\u0639 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0648\u0625\u0636\u0627\u0641\u0629 \u062a\u0641\u0627\u0635\u064a\u0644 \u0623\u0643\u062b\u0631 \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0633\u064a\u0627\u0642 \u0648\u0627\u0644\u0645\u0639\u0646\u0649:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText,
                "",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a \u0625\u0636\u0627\u0641\u064a\u0629: {customPrompt}"
            }),

            "summarize" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u062a\u0644\u062e\u064a\u0635 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0646\u0642\u0627\u0637 \u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629 \u0648\u0627\u0644\u0645\u0639\u0644\u0648\u0645\u0627\u062a \u0627\u0644\u0645\u0647\u0645\u0629:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText
            }),

            "formalize" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u0625\u0639\u0627\u062f\u0629 \u0635\u064a\u0627\u063a\u0629 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0628\u0644\u063a\u0629 \u0631\u0633\u0645\u064a\u0629 \u062d\u0643\u0648\u0645\u064a\u0629 \u0633\u0639\u0648\u062f\u064a\u0629 \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0645\u0639\u0646\u0649:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText
            }),

            "custom" => string.Join("\n", new[]
            {
                customPrompt,
                "",
                string.IsNullOrEmpty(currentText) ? "" : $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:\n{currentText}",
                "",
                $"\u0627\u0633\u0645 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectName}",
                $"\u0627\u0633\u0645 \u0627\u0644\u062d\u0642\u0644: {fieldName}"
            }),

            _ => string.Join("\n", new[]
            {
                $"\u0642\u0645 \u0628\u0643\u062a\u0627\u0628\u0629 \u0645\u062d\u062a\u0648\u0649 \u0645\u0646\u0627\u0633\u0628 \u0644\u062d\u0642\u0644 \"{fieldName}\".",
                string.IsNullOrEmpty(currentText) ? "" : $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a: {currentText}",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a: {customPrompt}"
            })
        };
    }

    /// <summary>
    /// Clean the AI response to remove any markdown or HTML formatting
    /// </summary>
    private static string CleanResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Remove markdown bold/italic
        text = Regex.Replace(text, @"\*{1,3}([^*]+)\*{1,3}", "$1");
        // Remove markdown headers
        text = Regex.Replace(text, @"^#{1,6}\s*", "", RegexOptions.Multiline);
        // Remove HTML tags
        text = Regex.Replace(text, @"<[^>]+>", "");

        return text.Trim();
    }
}

public sealed record AiTextAssistRequest
{
    public string Action { get; init; } = "generate";
    public string? CurrentText { get; init; }
    public string? FieldName { get; init; }
    public string? FieldPurpose { get; init; }
    public string? ProjectName { get; init; }
    public string? ProjectDescription { get; init; }
    public string? CompetitionType { get; init; }
    public string? AdditionalContext { get; init; }
    public string? CustomPrompt { get; init; }
    public string Language { get; init; } = "ar";
}
