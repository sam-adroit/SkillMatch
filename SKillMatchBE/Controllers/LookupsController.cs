using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.DTOs.Catalog;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize]
public sealed class LookupsController(ILookupService lookups) : ControllerBase
{
    [HttpGet("api/skills")]
    public Task<IReadOnlyList<LookupResponse>> GetSkills(CancellationToken token) => lookups.GetAsync(LookupKind.Skill, token);

    [HttpGet("api/interests")]
    public Task<IReadOnlyList<LookupResponse>> GetInterests(CancellationToken token) => lookups.GetAsync(LookupKind.Interest, token);

    [HttpGet("api/categories")]
    public Task<IReadOnlyList<LookupResponse>> GetCategories(CancellationToken token) => lookups.GetAsync(LookupKind.Category, token);

    [Authorize(Roles = "Admin")]
    [HttpPost("api/admin/{kind}")]
    public async Task<IActionResult> Create(string kind, SaveLookupRequest request, CancellationToken token)
    {
        if (!TryKind(kind, out var parsed)) return NotFound();
        var result = await lookups.CreateAsync(parsed, request, token);
        return result.Failure == LookupServiceFailure.DuplicateName
            ? Conflict(Problem("Duplicate name", "A lookup with this name already exists.", 409))
            : StatusCode(StatusCodes.Status201Created, result.Lookup);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("api/admin/{kind}/{id:guid}")]
    public async Task<IActionResult> Update(string kind, Guid id, SaveLookupRequest request, CancellationToken token)
    {
        if (!TryKind(kind, out var parsed)) return NotFound();
        var result = await lookups.UpdateAsync(parsed, id, request, token);
        return result.Failure switch
        {
            LookupServiceFailure.None => Ok(result.Lookup),
            LookupServiceFailure.NotFound => NotFound(),
            LookupServiceFailure.DuplicateName => Conflict(Problem("Duplicate name", "A lookup with this name already exists.", 409)),
            _ => BadRequest()
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("api/admin/{kind}/{id:guid}")]
    public async Task<IActionResult> Delete(string kind, Guid id, CancellationToken token)
    {
        if (!TryKind(kind, out var parsed)) return NotFound();
        var result = await lookups.DeleteAsync(parsed, id, token);
        return result switch
        {
            LookupServiceFailure.None => NoContent(),
            LookupServiceFailure.NotFound => NotFound(),
            LookupServiceFailure.InUse => Conflict(Problem("Lookup is in use", "Remove this lookup from profiles and projects before deleting it.", 409)),
            _ => BadRequest()
        };
    }

    private static bool TryKind(string value, out LookupKind kind) => value.ToLowerInvariant() switch
    {
        "skills" => Return(LookupKind.Skill, out kind),
        "interests" => Return(LookupKind.Interest, out kind),
        "categories" => Return(LookupKind.Category, out kind),
        _ => Return(default, out kind, false)
    };

    private static bool Return(LookupKind value, out LookupKind kind, bool result = true) { kind = value; return result; }

    private static ProblemDetails Problem(string title, string detail, int status) => new() { Title = title, Detail = detail, Status = status };
}
