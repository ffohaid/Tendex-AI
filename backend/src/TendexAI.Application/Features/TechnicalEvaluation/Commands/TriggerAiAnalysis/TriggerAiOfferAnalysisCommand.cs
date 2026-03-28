using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.TriggerAiAnalysis;

/// <summary>
/// Command to trigger AI-powered analysis for all supplier offers in a technical evaluation.
/// Analyzes each offer against the competition's terms and specifications booklet (كراسة الشروط والمواصفات)
/// and generates structured recommendations for the examination committee.
/// 
/// Per RAG Guidelines:
/// - AI acts as assistant (Copilot), not decision maker
/// - Blind evaluation enforced (no supplier identity exposure)
/// - All outputs in Arabic
/// - Grounding & Citation: extract-then-analyze methodology
/// </summary>
public sealed record TriggerAiOfferAnalysisCommand(
    Guid EvaluationId,
    string TriggeredByUserId) : ICommand<AiAnalysisSummaryDto>;
