using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.ReviewAiAnalysis;

/// <summary>
/// Command to mark an AI offer analysis as reviewed by a human committee member.
/// Per RAG Guidelines: AI is Copilot, human makes the final decision.
/// The review marks the analysis as acknowledged without necessarily accepting all scores.
/// </summary>
public sealed record ReviewAiAnalysisCommand(
    Guid AnalysisId,
    string ReviewedByUserId,
    string? ReviewNotes) : ICommand<AiOfferAnalysisDto>;
