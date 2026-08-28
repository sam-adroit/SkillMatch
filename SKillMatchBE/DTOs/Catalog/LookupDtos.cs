using System.ComponentModel.DataAnnotations;

namespace SkillMatchBE.DTOs.Catalog;

public sealed record LookupResponse(Guid Id, string Name);

public sealed record SaveLookupRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name);
