using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Dtos;

/// <summary>
/// Extension methods for mapping Committee domain entities to DTOs.
/// </summary>
public static class CommitteeMappingExtensions
{
    /// <summary>Maps a Committee entity to a list item DTO.</summary>
    public static CommitteeListItemDto ToListItemDto(
        this Committee committee,
        string? competitionNameAr = null,
        string? competitionNameEn = null)
    {
        var daysRemaining = (int)(committee.EndDate - DateTime.UtcNow).TotalDays;
        var workloadScore = CalculateWorkloadScore(committee);

        return new CommitteeListItemDto(
            Id: committee.Id,
            NameAr: committee.NameAr,
            NameEn: committee.NameEn,
            Type: committee.Type,
            IsPermanent: committee.IsPermanent,
            Status: committee.Status,
            ActiveMemberCount: committee.ActiveMemberCount,
            StartDate: committee.StartDate,
            EndDate: committee.EndDate,
            CompetitionId: committee.CompetitionId,
            CompetitionNameAr: competitionNameAr,
            CompetitionNameEn: competitionNameEn,
            CreatedAt: committee.CreatedAt,
            DaysRemaining: daysRemaining,
            WorkloadScore: workloadScore);
    }

    /// <summary>Maps a Committee entity (with members) to a detail DTO.</summary>
    public static CommitteeDetailDto ToDetailDto(
        this Committee committee,
        string? competitionNameAr = null,
        string? competitionNameEn = null,
        CommitteeAiInsightDto? aiInsight = null)
    {
        var daysRemaining = (int)(committee.EndDate - DateTime.UtcNow).TotalDays;
        var workloadScore = CalculateWorkloadScore(committee);

        return new CommitteeDetailDto(
            Id: committee.Id,
            NameAr: committee.NameAr,
            NameEn: committee.NameEn,
            Type: committee.Type,
            IsPermanent: committee.IsPermanent,
            Description: committee.Description,
            Status: committee.Status,
            StartDate: committee.StartDate,
            EndDate: committee.EndDate,
            CompetitionId: committee.CompetitionId,
            CompetitionNameAr: competitionNameAr,
            CompetitionNameEn: competitionNameEn,
            ActiveFromPhase: committee.ActiveFromPhase,
            ActiveToPhase: committee.ActiveToPhase,
            StatusChangeReason: committee.StatusChangeReason,
            StatusChangedBy: committee.StatusChangedBy,
            StatusChangedAt: committee.StatusChangedAt,
            Members: committee.Members.Select(m => m.ToDto()).ToList().AsReadOnly(),
            CreatedAt: committee.CreatedAt,
            CreatedBy: committee.CreatedBy,
            DaysRemaining: daysRemaining,
            WorkloadScore: workloadScore,
            AiInsight: aiInsight);
    }

    /// <summary>Maps a CommitteeMember entity to a DTO.</summary>
    public static CommitteeMemberDto ToDto(this CommitteeMember member)
    {
        return new CommitteeMemberDto(
            Id: member.Id,
            UserId: member.UserId,
            UserFullName: member.UserFullName,
            Role: member.Role,
            ActiveFromPhase: member.ActiveFromPhase,
            ActiveToPhase: member.ActiveToPhase,
            IsActive: member.IsActive,
            AssignedAt: member.AssignedAt,
            AssignedBy: member.AssignedBy,
            RemovedAt: member.RemovedAt,
            RemovedBy: member.RemovedBy,
            RemovalReason: member.RemovalReason);
    }

    /// <summary>
    /// Calculates a workload score for the committee based on member count,
    /// days remaining, and status.
    /// Score ranges from 0.0 (low workload) to 1.0 (high workload/risk).
    /// </summary>
    private static double CalculateWorkloadScore(Committee committee)
    {
        if (committee.Status == CommitteeStatus.Dissolved)
            return 0.0;

        var score = 0.0;

        // Factor 1: Member count (fewer members = higher workload per member)
        if (committee.ActiveMemberCount == 0)
            score += 0.4;
        else if (committee.ActiveMemberCount < 3)
            score += 0.25;
        else if (committee.ActiveMemberCount < 5)
            score += 0.1;

        // Factor 2: Days remaining (fewer days = higher urgency)
        var daysRemaining = (committee.EndDate - DateTime.UtcNow).TotalDays;
        if (daysRemaining < 0)
            score += 0.35;
        else if (daysRemaining < 7)
            score += 0.25;
        else if (daysRemaining < 30)
            score += 0.15;
        else
            score += 0.05;

        // Factor 3: No chair assigned
        var hasChair = committee.Members.Any(m =>
            m.Role == CommitteeMemberRole.Chair && m.IsActive);
        if (!hasChair)
            score += 0.2;

        // Factor 4: Suspended status
        if (committee.Status == CommitteeStatus.Suspended)
            score += 0.15;

        return Math.Min(score, 1.0);
    }
}
