using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Application.Features.Committees.Dtos;

/// <summary>
/// Extension methods for mapping Committee domain entities to DTOs.
/// </summary>
public static class CommitteeMappingExtensions
{
    /// <summary>Maps a Committee entity to a list item DTO.</summary>
    public static CommitteeListItemDto ToListItemDto(this Committee committee)
    {
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
            CreatedAt: committee.CreatedAt);
    }

    /// <summary>Maps a Committee entity (with members) to a detail DTO.</summary>
    public static CommitteeDetailDto ToDetailDto(this Committee committee)
    {
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
            ActiveFromPhase: committee.ActiveFromPhase,
            ActiveToPhase: committee.ActiveToPhase,
            StatusChangeReason: committee.StatusChangeReason,
            StatusChangedBy: committee.StatusChangedBy,
            StatusChangedAt: committee.StatusChangedAt,
            Members: committee.Members.Select(m => m.ToDto()).ToList().AsReadOnly(),
            CreatedAt: committee.CreatedAt,
            CreatedBy: committee.CreatedBy);
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
}
