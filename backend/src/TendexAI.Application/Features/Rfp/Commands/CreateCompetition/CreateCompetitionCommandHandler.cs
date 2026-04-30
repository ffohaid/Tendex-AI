using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.CreateCompetition;

/// <summary>
/// Handles the creation of a new competition in the tenant database.
/// </summary>
public sealed class CreateCompetitionCommandHandler
    : ICommandHandler<CreateCompetitionCommand, CompetitionDetailDto>
{
    private const int ProjectNameMaxLength = 500;
    private const int DescriptionMaxLength = 4000;
    private const string UploadExtractFallbackProjectName = "مشروع مستخرج";

    private readonly ICompetitionRepository _repository;
    private readonly ILogger<CreateCompetitionCommandHandler> _logger;

    public CreateCompetitionCommandHandler(
        ICompetitionRepository repository,
        ILogger<CreateCompetitionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<CompetitionDetailDto>> Handle(
        CreateCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.BookletNumber) &&
            await _repository.IsReferenceNumberInUseAsync(request.BookletNumber, cancellationToken: cancellationToken))
        {
            return Result.Failure<CompetitionDetailDto>("رقم الكراسة المدخل مستخدم مسبقاً.");
        }

        var projectNameAr = request.ProjectNameAr;
        var projectNameEn = request.ProjectNameEn;
        var description = request.Description;

        if (request.CreationMethod == RfpCreationMethod.UploadExtract)
        {
            projectNameAr = SanitizeUploadExtractText(
                request.ProjectNameAr,
                ProjectNameMaxLength,
                UploadExtractFallbackProjectName)!;

            projectNameEn = SanitizeUploadExtractText(
                request.ProjectNameEn,
                ProjectNameMaxLength,
                projectNameAr)!;

            description = SanitizeUploadExtractText(
                request.Description,
                DescriptionMaxLength,
                fallbackValue: null);
        }

        var competition = Competition.Create(
            tenantId: request.TenantId,
            projectNameAr: projectNameAr,
            projectNameEn: projectNameEn,
            competitionType: request.CompetitionType,
            creationMethod: request.CreationMethod,
            createdByUserId: request.CreatedByUserId,
            referenceNumber: request.BookletNumber,
            description: description,
            estimatedBudget: request.EstimatedBudget,
            bookletIssueDate: request.BookletIssueDate,
            inquiriesStartDate: request.InquiriesStartDate,
            inquiryPeriodDays: request.InquiryPeriodDays,
            offersStartDate: request.OffersStartDate,
            submissionDeadline: request.SubmissionDeadline,
            expectedAwardDate: request.ExpectedAwardDate,
            workStartDate: request.WorkStartDate,
            department: request.Department,
            fiscalYear: request.FiscalYear,
            sourceTemplateId: request.SourceTemplateId,
            sourceCompetitionId: request.SourceCompetitionId);

        try
        {
            await _repository.AddAsync(competition, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (request.CreationMethod == RfpCreationMethod.UploadExtract)
        {
            _logger.LogWarning(ex,
                "Database update failed while creating upload-extracted competition for tenant {TenantId}",
                request.TenantId);

            return Result.Failure<CompetitionDetailDto>(
                "تعذر إنشاء المنافسة من البيانات المستخرجة. يرجى رفع الملف مرة أخرى أو استخدام ملف آخر.");
        }

        _logger.LogCompetitionCreated(competition.Id, competition.ReferenceNumber ?? string.Empty, request.TenantId);

        var created = await _repository.GetByIdWithDetailsAsync(competition.Id, cancellationToken);
        if (created is null)
        {
            _logger.LogWarning(
                "Competition {CompetitionId} was created but could not be reloaded with details. Returning base entity.",
                competition.Id);

            created = competition;
        }

        return Result.Success(CompetitionMapper.ToDetailDto(created));
    }

    private static string? SanitizeUploadExtractText(
        string? value,
        int maxLength,
        string? fallbackValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallbackValue;
        }

        var sanitized = new string(value
            .Where(ch => !char.IsControl(ch) || ch is '\r' or '\n' or '\t')
            .ToArray());

        sanitized = Regex.Replace(sanitized, "\\s+", " ").Trim();

        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength].Trim();
        }

        return string.IsNullOrWhiteSpace(sanitized) ? fallbackValue : sanitized;
    }
}
