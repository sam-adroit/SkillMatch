using System.ComponentModel.DataAnnotations;

namespace SkillMatchBE.DTOs.Workflows;

public sealed record ApplyToProjectRequest([StringLength(1000)] string? Note);

public sealed record DecideApplicationRequest(
    [Required] string Status,
    [StringLength(1000)] string? DecisionNote);

public sealed record ApplicationQuery(string? Status, Guid? ProjectId);

public sealed record ApplicationResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid ProjectId,
    string ProjectTitle,
    string ProjectStatus,
    string Note,
    string Status,
    DateTimeOffset AppliedAt,
    DateTimeOffset? DecidedAt,
    string DecisionNote);

public sealed record SaveTeamRequest(
    Guid ProjectId,
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    Guid LeaderStudentId,
    [Required, MinLength(1)] IReadOnlyList<Guid> MemberStudentIds);

public sealed record UpdateTeamRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    Guid LeaderStudentId,
    [Required, MinLength(1)] IReadOnlyList<Guid> MemberStudentIds);

public sealed record TeamMemberResponse(Guid StudentId, string Name, bool IsLeader, DateTimeOffset JoinedAt);

public sealed record TeamResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    string Name,
    string Status,
    int MaximumSize,
    IReadOnlyList<TeamMemberResponse> Members,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminDashboardResponse(
    int Students,
    int Projects,
    int Teams,
    int PendingApplications,
    int UnassignedStudents);
