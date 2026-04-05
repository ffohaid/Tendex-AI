using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Committees.Commands.AddCommitteeMember;
using TendexAI.Application.Features.Committees.Commands.ChangeCommitteeStatus;
using TendexAI.Application.Features.Committees.Commands.CreateCommittee;
using TendexAI.Application.Features.Committees.Commands.RemoveCommitteeMember;
using TendexAI.Application.Features.Committees.Commands.UpdateCommittee;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Application.Features.Committees.Queries.GetCommitteeById;
using TendexAI.Application.Features.Committees.Queries.GetCommitteesList;
using TendexAI.Application.Features.Committees.Queries.GetCommitteeStatistics;
using TendexAI.Application.Features.Committees.Queries.GetCommitteeAiAnalysis;
using TendexAI.Application.Features.Committees.Queries.GetCompetitionCommittees;
using TendexAI.Application.Features.Committees.Queries.GetEligibleUsers;
using TendexAI.Application.Features.Committees.Queries.ValidateConflictOfInterest;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Committees;

/// <summary>
/// Defines Minimal API endpoints for committee management.
/// Covers CRUD operations, member management, eligible user search,
/// and conflict of interest validation.
/// All endpoints operate against the tenant-specific database.
/// </summary>
public static class CommitteeEndpoints
{
    /// <summary>
    /// Maps all committee endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapCommitteeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/committees")
            .WithTags("Committees")
            .RequireAuthorization();

        // ═════════════════════════════════════════════════════════════
        //  Committee CRUD
        // ═════════════════════════════════════════════════════════════

        group.MapGet("/", GetCommitteesAsync)
            .WithName("GetCommittees")
            .WithSummary("Get paginated list of committees for the current tenant")
            .Produces<CommitteePagedResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{committeeId:guid}", GetCommitteeByIdAsync)
            .WithName("GetCommitteeById")
            .WithSummary("Get a specific committee with all members")
            .Produces<CommitteeDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.CommitteesView);

        group.MapPost("/", CreateCommitteeAsync)
            .WithName("CreateCommittee")
            .WithSummary("Create a new committee (permanent or temporary)")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(PermissionPolicies.CommitteesCreate);

        group.MapPut("/{committeeId:guid}", UpdateCommitteeAsync)
            .WithName("UpdateCommittee")
            .WithSummary("Update committee information, scope, and competition links")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.CommitteesEdit);

        group.MapPut("/{committeeId:guid}/status", ChangeCommitteeStatusAsync)
            .WithName("ChangeCommitteeStatus")
            .WithSummary("Change committee lifecycle status (Suspend, Reactivate, Dissolve)")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ═════════════════════════════════════════════════════════════
        //  Member Management
        // ═════════════════════════════════════════════════════════════

        group.MapGet("/{committeeId:guid}/eligible-users", GetEligibleUsersAsync)
            .WithName("GetEligibleUsers")
            .WithSummary("Search for platform users eligible to be added to a committee")
            .Produces<IReadOnlyList<EligibleUserDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{committeeId:guid}/members", AddCommitteeMemberAsync)
            .WithName("AddCommitteeMember")
            .WithSummary("Add a registered platform user to a committee with validation")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .RequireAuthorization(PermissionPolicies.CommitteesManageMembers);

        group.MapDelete("/{committeeId:guid}/members/{userId:guid}", RemoveCommitteeMemberAsync)
            .WithName("RemoveCommitteeMember")
            .WithSummary("Remove (deactivate) a member from a committee")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.CommitteesManageMembers);

        // ═════════════════════════════════════════════════════════════
        //  Conflict of Interest Validation
        // ═════════════════════════════════════════════════════════════

        group.MapGet("/{committeeId:guid}/conflict-check/{userId:guid}", ValidateConflictOfInterestAsync)
            .WithName("ValidateConflictOfInterest")
            .WithSummary("Check if adding a user to a committee would violate conflict of interest rules")
            .Produces<ConflictOfInterestResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ═════════════════════════════════════════════════════════════
        //  Statistics & AI Analysis
        // ═════════════════════════════════════════════════════════════

        group.MapGet("/statistics", GetCommitteeStatisticsAsync)
            .WithName("GetCommitteeStatistics")
            .WithSummary("Get committee statistics for the current tenant")
            .Produces<CommitteeStatisticsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{committeeId:guid}/ai-analysis", GetCommitteeAiAnalysisAsync)
            .WithName("GetCommitteeAiAnalysis")
            .WithSummary("Get AI-powered analysis and recommendations for a committee")
            .Produces<CommitteeAiAnalysisResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ═════════════════════════════════════════════════════════════
        //  Competition-Scoped Queries
        // ═════════════════════════════════════════════════════════════

        app.MapGet("/api/v1/competitions/{competitionId:guid}/committees", GetCompetitionCommitteesAsync)
            .WithTags("Committees")
            .WithName("GetCompetitionCommittees")
            .WithSummary("Get all committees linked to a specific competition")
            .RequireAuthorization()
            .Produces<IReadOnlyList<CommitteeDetailDto>>(StatusCodes.Status200OK);

        return app;
    }

    // ═════════════════════════════════════════════════════════════
    //  Endpoint Handlers
    // ═════════════════════════════════════════════════════════════

    private static async Task<IResult> GetCommitteesAsync(
        [AsParameters] GetCommitteesListQueryParams queryParams,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCommitteesListQuery(
            PageNumber: queryParams.PageNumber ?? 1,
            PageSize: queryParams.PageSize ?? 20,
            TypeFilter: queryParams.Type,
            StatusFilter: queryParams.Status,
            IsPermanentFilter: queryParams.IsPermanent,
            CompetitionIdFilter: queryParams.CompetitionId,
            SearchTerm: queryParams.Search);

        var result = await sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetCommitteeByIdAsync(
        Guid committeeId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCommitteeByIdQuery(committeeId), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateCommitteeAsync(
        CreateCommitteeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateCommitteeCommand(
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            Type: request.Type,
            IsPermanent: request.IsPermanent,
            ScopeType: request.ScopeType,
            Description: request.Description,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            CompetitionIds: request.CompetitionIds,
            Phases: request.Phases);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/committees/{result.Value}", result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> UpdateCommitteeAsync(
        Guid committeeId,
        UpdateCommitteeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCommitteeCommand(
            CommitteeId: committeeId,
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            Description: request.Description,
            ScopeType: request.ScopeType,
            Phases: request.Phases,
            CompetitionIds: request.CompetitionIds);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ChangeCommitteeStatusAsync(
        Guid committeeId,
        ChangeCommitteeStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ChangeCommitteeStatusCommand(
            CommitteeId: committeeId,
            NewStatus: request.NewStatus,
            Reason: request.Reason);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetEligibleUsersAsync(
        Guid committeeId,
        [FromQuery] CommitteeMemberRole? role,
        [FromQuery] string? search,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetEligibleUsersQuery(
            CommitteeId: committeeId,
            Role: role,
            SearchTerm: search);

        var result = await sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AddCommitteeMemberAsync(
        Guid committeeId,
        AddCommitteeMemberRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AddCommitteeMemberCommand(
            CommitteeId: committeeId,
            UserId: request.UserId,
            Role: request.Role);

        var result = await sender.Send(command, cancellationToken);
        if (result.IsSuccess)
            return Results.Ok();

        // Conflict of interest violations return 409 Conflict
        if (result.Error?.Contains("Conflict of interest") == true)
            return Results.Problem(result.Error, statusCode: StatusCodes.Status409Conflict);

        return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RemoveCommitteeMemberAsync(
        Guid committeeId,
        Guid userId,
        [FromQuery] string reason,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RemoveCommitteeMemberCommand(
            CommitteeId: committeeId,
            UserId: userId,
            Reason: reason);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ValidateConflictOfInterestAsync(
        Guid committeeId,
        Guid userId,
        [FromQuery] CommitteeMemberRole role,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new ValidateConflictOfInterestQuery(
            UserId: userId,
            CommitteeId: committeeId,
            Role: role);

        var result = await sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCommitteeStatisticsAsync(
        [FromQuery] bool? isPermanent,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCommitteeStatisticsQuery(IsPermanentFilter: isPermanent);
        var result = await sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetCommitteeAiAnalysisAsync(
        Guid committeeId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCommitteeAiAnalysisQuery(CommitteeId: committeeId);
        var result = await sender.Send(query, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetCompetitionCommitteesAsync(
        Guid competitionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCompetitionCommitteesQuery(competitionId), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }
}

// ═════════════════════════════════════════════════════════════
//  Request DTOs (API layer only)
// ═════════════════════════════════════════════════════════════

/// <summary>Query parameters for the GetCommittees endpoint.</summary>
public sealed record GetCommitteesListQueryParams(
    int? PageNumber,
    int? PageSize,
    CommitteeType? Type,
    CommitteeStatus? Status,
    bool? IsPermanent,
    Guid? CompetitionId,
    string? Search);

/// <summary>Request body for creating a new committee.</summary>
public sealed record CreateCommitteeRequest(
    string NameAr,
    string NameEn,
    CommitteeType Type,
    bool IsPermanent,
    CommitteeScopeType ScopeType,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    List<Guid>? CompetitionIds,
    List<CompetitionPhase>? Phases);

/// <summary>Request body for updating committee information.</summary>
public sealed record UpdateCommitteeRequest(
    string NameAr,
    string NameEn,
    string? Description,
    CommitteeScopeType ScopeType,
    List<CompetitionPhase>? Phases,
    List<Guid>? CompetitionIds);

/// <summary>Request body for changing committee status.</summary>
public sealed record ChangeCommitteeStatusRequest(
    CommitteeStatus NewStatus,
    string? Reason);

/// <summary>Request body for adding a registered platform user to a committee.</summary>
public sealed record AddCommitteeMemberRequest(
    Guid UserId,
    CommitteeMemberRole Role);
