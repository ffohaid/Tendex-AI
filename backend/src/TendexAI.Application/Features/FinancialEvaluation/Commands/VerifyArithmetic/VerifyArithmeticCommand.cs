using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.VerifyArithmetic;

/// <summary>
/// Command to verify arithmetic correctness of all financial offer items for a supplier.
/// Per PRD Section 10.2 — arithmetic verification.
/// </summary>
public sealed record VerifyArithmeticCommand(
    Guid CompetitionId,
    Guid SupplierOfferId,
    string VerifiedByUserId) : ICommand<ArithmeticVerificationResultDto>;
