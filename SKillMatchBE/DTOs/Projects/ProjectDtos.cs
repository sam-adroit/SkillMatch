using System.ComponentModel.DataAnnotations;
using SkillMatchBE.DTOs.Catalog;

namespace SkillMatchBE.DTOs.Projects;

public sealed record SaveProjectRequest(
    [Required, StringLength(160, MinimumLength = 3)] string Title,
    [Required, StringLength(4000, MinimumLength = 20)] string Description,
    [Required] string Difficulty,
    Guid CategoryId,
    [Range(1, 20)] int MinimumTeamSize,
    [Range(1, 20)] int PreferredTeamSize,
    [Range(1, 20)] int MaximumTeamSize,
    [Required, MinLength(1)] IReadOnlyList<Guid> RequiredSkillIds,
    [StringLength(2000)] string? AdminNotes);

public sealed record ProjectResponse(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    string Status,
    int MinimumTeamSize,
    int PreferredTeamSize,
    int MaximumTeamSize,
    LookupResponse Category,
    IReadOnlyList<LookupResponse> RequiredSkills,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminProjectResponse(
    Guid Id,
    string Title,
    string Description,
    string AdminNotes,
    string Difficulty,
    string Status,
    int MinimumTeamSize,
    int PreferredTeamSize,
    int MaximumTeamSize,
    LookupResponse Category,
    IReadOnlyList<LookupResponse> RequiredSkills,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ChangeProjectStatusRequest([Required] string Status);

public sealed record ProjectQuery(
    string? Search,
    Guid? SkillId,
    Guid? CategoryId,
    string? Difficulty,
    bool? Available,
    int? TeamSize);
