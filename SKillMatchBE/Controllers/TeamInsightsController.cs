using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize]
[Route("api/teams")]
public sealed class TeamInsightsController(IRecommendationService recommendations) : ControllerBase
{
    [HttpGet("{teamId:guid}/skill-gaps")]
    public async Task<IActionResult> SkillGaps(Guid teamId, CancellationToken token)
    {
        var result = await recommendations.GetTeamSkillGapsAsync(teamId, CurrentUserId(), User.IsInRole("Admin"), token);
        return result.Failure switch
        {
            RecommendationFailure.None => Ok(result.Value),
            RecommendationFailure.NotFound => NotFound(),
            RecommendationFailure.Forbidden => Forbid(),
            _ => UnprocessableEntity()
        };
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
