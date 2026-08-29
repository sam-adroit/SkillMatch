using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/recommendations")]
public sealed class RecommendationsController(IRecommendationService recommendations) : ControllerBase
{
    [HttpPost("projects")]
    public async Task<IActionResult> Projects(CancellationToken token) =>
        Map(await recommendations.RecommendProjectsAsync(CurrentUserId(), token));

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken token) =>
        Map(await recommendations.GetHistoryAsync(CurrentUserId(), token));

    [HttpGet("teammates")]
    public async Task<IActionResult> Teammates(CancellationToken token) =>
        Map(await recommendations.SuggestTeammatesAsync(CurrentUserId(), token));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult Map<T>(RecommendationResult<T> result) => result.Failure switch
    {
        RecommendationFailure.None => Ok(result.Value),
        RecommendationFailure.MissingProfile => UnprocessableEntity(Problem("Profile required", result.Detail!, 422)),
        RecommendationFailure.InsufficientProfile => UnprocessableEntity(Problem("Profile needs more detail", result.Detail!, 422)),
        RecommendationFailure.NotFound => NotFound(),
        RecommendationFailure.Forbidden => Forbid(),
        _ => UnprocessableEntity()
    };

    private static ProblemDetails Problem(string title, string detail, int status) => new()
    {
        Title = title,
        Detail = detail,
        Status = status
    };
}
