using System.Text.RegularExpressions;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// Minimal API endpoint for general-purpose AI text assistance.
/// Provides text generation, improvement, expansion, summarization, and formalization.
///
/// Issue 19 Fix: Added field-specific constraints to prevent AI from generating
///   irrelevant content (e.g., general text in a date field).
/// Issue 20 Fix: Added character limit enforcement in prompts and post-processing
///   to ensure generated content respects field specifications (e.g., max 2000 chars).
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

            // Issue 20: Determine appropriate max tokens based on field constraints
            var maxTokens = DetermineMaxTokens(request);

            var aiRequest = new AiCompletionRequest
            {
                TenantId = tenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                MaxTokensOverride = maxTokens,
                TemperatureOverride = 0.4
            };

            var response = await gateway.GenerateCompletionAsync(aiRequest, ct);

            if (response.IsSuccess)
            {
                var generatedText = CleanResponse(response.Content ?? "");

                // Issue 20: Enforce character limit if specified
                if (request.MaxCharacters > 0 && generatedText.Length > request.MaxCharacters)
                {
                    generatedText = TruncateToLimit(generatedText, request.MaxCharacters);
                }

                return Results.Ok(new
                {
                    IsSuccess = true,
                    GeneratedText = generatedText,
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

    /// <summary>
    /// Issue 19 Fix: Build field-aware system prompt that constrains AI output
    /// to match the specific field type and purpose.
    /// </summary>
    private static string BuildSystemPrompt(AiTextAssistRequest request)
    {
        var fieldName = request.FieldName ?? "";
        var fieldPurpose = request.FieldPurpose ?? "";
        var projectName = request.ProjectName ?? "";
        var competitionType = request.CompetitionType ?? "";

        // Issue 19: Detect date-related fields and add strict constraints
        var fieldConstraints = GetFieldSpecificConstraints(fieldName, request.MaxCharacters);

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
            "8. \u0623\u0639\u062f \u0627\u0644\u0645\u062d\u062a\u0648\u0649 \u0627\u0644\u0645\u0646\u0627\u0633\u0628 \u0644\u0644\u062d\u0642\u0644 \u0627\u0644\u0645\u0637\u0644\u0648\u0628 \u0641\u0642\u0637 - \u0644\u0627 \u062a\u0636\u0641 \u0645\u062d\u062a\u0648\u0649 \u063a\u064a\u0631 \u0645\u0637\u0644\u0648\u0628 \u0623\u0648 \u063a\u064a\u0631 \u0645\u0631\u062a\u0628\u0637 \u0628\u0627\u0644\u062d\u0642\u0644",
            "",
            // Issue 19: Add field-specific constraints
            fieldConstraints,
            "",
            "\u0627\u0644\u0633\u064a\u0627\u0642:",
            $"- \u0627\u0633\u0645 \u0627\u0644\u062d\u0642\u0644: {fieldName}",
            $"- \u0627\u0644\u063a\u0631\u0636 \u0645\u0646 \u0627\u0644\u062d\u0642\u0644: {fieldPurpose}",
            $"- \u0627\u0633\u0645 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectName}",
            $"- \u0646\u0648\u0639 \u0627\u0644\u0645\u0646\u0627\u0641\u0633\u0629: {competitionType}"
        });
    }

    /// <summary>
    /// Issue 19: Get field-specific constraints based on field name to prevent
    /// AI from generating irrelevant content for specific field types.
    /// </summary>
    private static string GetFieldSpecificConstraints(string fieldName, int maxCharacters)
    {
        var lowerFieldName = fieldName.ToLowerInvariant();
        var constraints = new List<string>();

        // Date-related fields
        if (lowerFieldName.Contains("\u062a\u0627\u0631\u064a\u062e") || lowerFieldName.Contains("date") ||
            lowerFieldName.Contains("\u0637\u0631\u062d") || lowerFieldName.Contains("\u0645\u0648\u0639\u062f"))
        {
            constraints.Add("\u062a\u0646\u0628\u064a\u0647 \u0645\u0647\u0645: \u0647\u0630\u0627 \u0627\u0644\u062d\u0642\u0644 \u0645\u062e\u0635\u0635 \u0644\u0644\u062a\u0627\u0631\u064a\u062e \u0641\u0642\u0637.");
            constraints.Add("\u0623\u0639\u062f \u062a\u0627\u0631\u064a\u062e\u0627\u064b \u0645\u0646\u0627\u0633\u0628\u0627\u064b \u0628\u0635\u064a\u063a\u0629 YYYY-MM-DD \u0641\u0642\u0637.");
            constraints.Add("\u0644\u0627 \u062a\u0643\u062a\u0628 \u0623\u064a \u0646\u0635 \u0623\u0648 \u0634\u0631\u062d \u0625\u0636\u0627\u0641\u064a - \u0641\u0642\u0637 \u0627\u0644\u062a\u0627\u0631\u064a\u062e.");
            constraints.Add("\u0645\u062b\u0627\u0644: 2025-06-15");
        }
        // Numeric/budget fields
        else if (lowerFieldName.Contains("\u0645\u064a\u0632\u0627\u0646\u064a\u0629") || lowerFieldName.Contains("budget") ||
                 lowerFieldName.Contains("\u0642\u064a\u0645\u0629") || lowerFieldName.Contains("value") ||
                 lowerFieldName.Contains("\u0645\u0628\u0644\u063a") || lowerFieldName.Contains("amount"))
        {
            constraints.Add("\u062a\u0646\u0628\u064a\u0647 \u0645\u0647\u0645: \u0647\u0630\u0627 \u0627\u0644\u062d\u0642\u0644 \u0645\u062e\u0635\u0635 \u0644\u0644\u0642\u064a\u0645\u0629 \u0627\u0644\u0631\u0642\u0645\u064a\u0629 \u0641\u0642\u0637.");
            constraints.Add("\u0623\u0639\u062f \u0631\u0642\u0645\u0627\u064b \u0641\u0642\u0637 \u0628\u062f\u0648\u0646 \u0623\u064a \u0646\u0635 \u0625\u0636\u0627\u0641\u064a.");
        }
        // Reference number fields
        else if (lowerFieldName.Contains("\u0631\u0642\u0645 \u0645\u0631\u062c\u0639\u064a") || lowerFieldName.Contains("reference"))
        {
            constraints.Add("\u062a\u0646\u0628\u064a\u0647 \u0645\u0647\u0645: \u0647\u0630\u0627 \u0627\u0644\u062d\u0642\u0644 \u0645\u062e\u0635\u0635 \u0644\u0644\u0631\u0642\u0645 \u0627\u0644\u0645\u0631\u062c\u0639\u064a \u0641\u0642\u0637.");
            constraints.Add("\u0623\u0639\u062f \u0631\u0642\u0645\u0627\u064b \u0645\u0631\u062c\u0639\u064a\u0627\u064b \u0645\u0646\u0627\u0633\u0628\u0627\u064b \u0641\u0642\u0637.");
        }
        // Description fields with character limits
        else if (lowerFieldName.Contains("\u0648\u0635\u0641") || lowerFieldName.Contains("description"))
        {
            if (maxCharacters > 0)
            {
                constraints.Add($"\u062a\u0646\u0628\u064a\u0647 \u0645\u0647\u0645: \u0627\u0644\u062d\u062f \u0627\u0644\u0623\u0642\u0635\u0649 \u0644\u0644\u0646\u0635 \u0647\u0648 {maxCharacters} \u062d\u0631\u0641.");
                constraints.Add($"\u064a\u062c\u0628 \u0623\u0646 \u064a\u0643\u0648\u0646 \u0627\u0644\u0646\u0635 \u0627\u0644\u0645\u0648\u0644\u062f \u0623\u0642\u0644 \u0645\u0646 {maxCharacters} \u062d\u0631\u0641 \u0628\u0634\u0643\u0644 \u0642\u0627\u0637\u0639.");
                constraints.Add("\u0627\u0643\u062a\u0628 \u0648\u0635\u0641\u0627\u064b \u0645\u0648\u062c\u0632\u0627\u064b \u0648\u0634\u0627\u0645\u0644\u0627\u064b \u0636\u0645\u0646 \u0627\u0644\u062d\u062f \u0627\u0644\u0645\u0637\u0644\u0648\u0628.");
            }
        }

        // General character limit constraint
        if (maxCharacters > 0 && constraints.Count == 0)
        {
            constraints.Add($"\u062a\u0646\u0628\u064a\u0647 \u0645\u0647\u0645: \u0627\u0644\u062d\u062f \u0627\u0644\u0623\u0642\u0635\u0649 \u0644\u0644\u0646\u0635 \u0647\u0648 {maxCharacters} \u062d\u0631\u0641. \u064a\u062c\u0628 \u0623\u0644\u0627 \u064a\u062a\u062c\u0627\u0648\u0632 \u0627\u0644\u0646\u0635 \u0627\u0644\u0645\u0648\u0644\u062f \u0647\u0630\u0627 \u0627\u0644\u062d\u062f.");
        }

        return constraints.Count > 0
            ? "\u0642\u064a\u0648\u062f \u062e\u0627\u0635\u0629 \u0628\u0627\u0644\u062d\u0642\u0644:\n" + string.Join("\n", constraints)
            : "";
    }

    /// <summary>
    /// Issue 20: Determine appropriate max tokens based on field character limit.
    /// </summary>
    private static int DetermineMaxTokens(AiTextAssistRequest request)
    {
        if (request.MaxCharacters > 0)
        {
            // Approximate: 1 Arabic character ≈ 2-3 tokens, add some buffer
            // But cap at reasonable limits
            var estimatedTokens = (int)(request.MaxCharacters * 0.8);
            return Math.Min(Math.Max(estimatedTokens, 200), 4000);
        }

        return 4000; // Default
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

        // Issue 20: Add character limit reminder in user prompt
        var charLimitNote = request.MaxCharacters > 0
            ? $"\n\u0645\u0644\u0627\u062d\u0638\u0629 \u0645\u0647\u0645\u0629: \u064a\u062c\u0628 \u0623\u0644\u0627 \u064a\u062a\u062c\u0627\u0648\u0632 \u0627\u0644\u0646\u0635 {request.MaxCharacters} \u062d\u0631\u0641."
            : "";

        return action switch
        {
            "generate" => string.Join("\n", new[]
            {
                $"\u0642\u0645 \u0628\u0643\u062a\u0627\u0628\u0629 \u0645\u062d\u062a\u0648\u0649 \u0627\u062d\u062a\u0631\u0627\u0641\u064a \u0648\u0645\u0641\u0635\u0644 \u0644\u062d\u0642\u0644 \"{fieldName}\" \u0641\u064a \u0643\u0631\u0627\u0633\u0629 \u0627\u0644\u0634\u0631\u0648\u0637 \u0648\u0627\u0644\u0645\u0648\u0627\u0635\u0641\u0627\u062a.",
                $"\u0627\u0633\u0645 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectName}",
                $"\u0648\u0635\u0641 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectDescription}",
                string.IsNullOrEmpty(additionalContext) ? "" : $"\u0633\u064a\u0627\u0642 \u0625\u0636\u0627\u0641\u064a: {additionalContext}",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a \u0625\u0636\u0627\u0641\u064a\u0629: {customPrompt}",
                charLimitNote
            }),

            "improve" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u062a\u062d\u0633\u064a\u0646 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0645\u0639\u0646\u0649 \u0627\u0644\u0623\u0633\u0627\u0633\u064a \u0648\u062a\u0639\u0632\u064a\u0632 \u0627\u0644\u062c\u0648\u062f\u0629 \u0648\u0627\u0644\u0645\u0647\u0646\u064a\u0629:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText,
                "",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a \u0625\u0636\u0627\u0641\u064a\u0629: {customPrompt}",
                charLimitNote
            }),

            "expand" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u062a\u0648\u0633\u064a\u0639 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0648\u0625\u0636\u0627\u0641\u0629 \u062a\u0641\u0627\u0635\u064a\u0644 \u0623\u0643\u062b\u0631 \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0633\u064a\u0627\u0642 \u0648\u0627\u0644\u0645\u0639\u0646\u0649:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText,
                "",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a \u0625\u0636\u0627\u0641\u064a\u0629: {customPrompt}",
                charLimitNote
            }),

            "summarize" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u062a\u0644\u062e\u064a\u0635 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0646\u0642\u0627\u0637 \u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629 \u0648\u0627\u0644\u0645\u0639\u0644\u0648\u0645\u0627\u062a \u0627\u0644\u0645\u0647\u0645\u0629:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText,
                charLimitNote
            }),

            "formalize" => string.Join("\n", new[]
            {
                "\u0642\u0645 \u0628\u0625\u0639\u0627\u062f\u0629 \u0635\u064a\u0627\u063a\u0629 \u0627\u0644\u0646\u0635 \u0627\u0644\u062a\u0627\u0644\u064a \u0628\u0644\u063a\u0629 \u0631\u0633\u0645\u064a\u0629 \u062d\u0643\u0648\u0645\u064a\u0629 \u0633\u0639\u0648\u062f\u064a\u0629 \u0645\u0639 \u0627\u0644\u062d\u0641\u0627\u0638 \u0639\u0644\u0649 \u0627\u0644\u0645\u0639\u0646\u0649:",
                "",
                $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:",
                currentText,
                charLimitNote
            }),

            "custom" => string.Join("\n", new[]
            {
                customPrompt,
                "",
                string.IsNullOrEmpty(currentText) ? "" : $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a:\n{currentText}",
                "",
                $"\u0627\u0633\u0645 \u0627\u0644\u0645\u0634\u0631\u0648\u0639: {projectName}",
                $"\u0627\u0633\u0645 \u0627\u0644\u062d\u0642\u0644: {fieldName}",
                charLimitNote
            }),

            _ => string.Join("\n", new[]
            {
                $"\u0642\u0645 \u0628\u0643\u062a\u0627\u0628\u0629 \u0645\u062d\u062a\u0648\u0649 \u0645\u0646\u0627\u0633\u0628 \u0644\u062d\u0642\u0644 \"{fieldName}\".",
                string.IsNullOrEmpty(currentText) ? "" : $"\u0627\u0644\u0646\u0635 \u0627\u0644\u062d\u0627\u0644\u064a: {currentText}",
                string.IsNullOrEmpty(customPrompt) ? "" : $"\u062a\u0639\u0644\u064a\u0645\u0627\u062a: {customPrompt}",
                charLimitNote
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

    /// <summary>
    /// Issue 20: Truncate text to the specified character limit while preserving
    /// complete sentences where possible.
    /// </summary>
    private static string TruncateToLimit(string text, int maxCharacters)
    {
        if (text.Length <= maxCharacters) return text;

        // Try to truncate at the last complete sentence within the limit
        var truncated = text[..maxCharacters];
        var lastPeriod = truncated.LastIndexOf('.');
        var lastNewline = truncated.LastIndexOf('\n');
        var lastBreak = Math.Max(lastPeriod, lastNewline);

        if (lastBreak > maxCharacters * 0.7) // Only use sentence break if it's at least 70% of the limit
        {
            return truncated[..(lastBreak + 1)].TrimEnd();
        }

        // Otherwise just truncate at the limit
        return truncated.TrimEnd();
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
    /// <summary>
    /// Issue 20: Maximum characters allowed for the generated text.
    /// When set, the AI prompt will include this constraint and the response
    /// will be truncated if it exceeds this limit.
    /// </summary>
    public int MaxCharacters { get; init; }
}
