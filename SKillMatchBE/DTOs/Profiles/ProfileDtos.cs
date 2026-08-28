using System.ComponentModel.DataAnnotations;
using SkillMatchBE.DTOs.Catalog;

namespace SkillMatchBE.DTOs.Profiles;

public sealed record StudentProfileResponse(
    Guid UserId,
    string Email,
    string ExperienceLevel,
    string Goals,
    IReadOnlyList<string> PreferredTechnologies,
    IReadOnlyList<LookupResponse> Skills,
    IReadOnlyList<LookupResponse> Interests,
    int CompletenessPercent,
    IReadOnlyList<string> MissingFields,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateStudentProfileRequest(
    [Required] string ExperienceLevel,
    [Required, StringLength(1000, MinimumLength = 10)] string Goals,
    [Required, MinLength(1)] IReadOnlyList<string> PreferredTechnologies,
    [Required, MinLength(1)] IReadOnlyList<Guid> SkillIds,
    [Required, MinLength(1)] IReadOnlyList<Guid> InterestIds);
