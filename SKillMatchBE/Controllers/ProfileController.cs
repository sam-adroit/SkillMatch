using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.DTOs.Profiles;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/profile")]
public sealed class ProfileController(IProfileService profiles) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<StudentProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var profile = await profiles.GetAsync(userId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType<StudentProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(UpdateStudentProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await profiles.UpdateAsync(userId, request, cancellationToken);
        return result.Failure switch
        {
            ProfileFailure.None => Ok(result.Profile),
            ProfileFailure.InvalidExperienceLevel => Validation("ExperienceLevel", "Choose Beginner, Intermediate, or Advanced."),
            ProfileFailure.InvalidLookup => Validation("Lookups", "One or more selected skills or interests do not exist."),
            _ => BadRequest()
        };
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private UnprocessableEntityObjectResult Validation(string field, string message) =>
        UnprocessableEntity(new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] })
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Profile validation failed"
        });
}
