using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.DTOs.Workflows;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize]
public sealed class TeamsController(IWorkflowService workflows) : ControllerBase
{
    [HttpGet("api/teams")]
    public Task<IReadOnlyList<TeamResponse>> Get(CancellationToken token) =>
        User.IsInRole("Admin")
            ? workflows.GetTeamsAsync(token)
            : workflows.GetStudentTeamsAsync(CurrentUserId(), token);

    [HttpGet("api/teams/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token)
    {
        var teams = User.IsInRole("Admin")
            ? await workflows.GetTeamsAsync(token)
            : await workflows.GetStudentTeamsAsync(CurrentUserId(), token);
        var team = teams.SingleOrDefault(item => item.Id == id);
        return team is null ? NotFound() : Ok(team);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("api/admin/teams")]
    public async Task<IActionResult> Create(SaveTeamRequest request, CancellationToken token) =>
        Map(await workflows.CreateTeamAsync(request, token), StatusCodes.Status201Created);

    [Authorize(Roles = "Admin")]
    [HttpPut("api/admin/teams/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTeamRequest request, CancellationToken token) =>
        Map(await workflows.UpdateTeamAsync(id, request, token));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult Map<T>(WorkflowResult<T> result, int successStatus = StatusCodes.Status200OK) =>
        result.Failure == WorkflowFailure.None
            ? StatusCode(successStatus, result.Value)
            : result.Failure switch
            {
                WorkflowFailure.NotFound => NotFound(),
                WorkflowFailure.ProjectClosed => Conflict(Problem("Project unavailable", "Teams can only be changed for a published project.", 409)),
                WorkflowFailure.CapacityReached => Conflict(Problem("Team capacity reached", "The requested members exceed the project's maximum team size.", 409)),
                WorkflowFailure.ExistingAssignment => Conflict(Problem("Existing team assignment", "A selected Student already belongs to another active team.", 409)),
                WorkflowFailure.TeamExists => Conflict(Problem("Team already exists", "This project already has an active team.", 409)),
                WorkflowFailure.ApplicationNotApproved => UnprocessableEntity(Problem("Approval required", "Every team member must have an Approved application for this project.", 422)),
                WorkflowFailure.LeaderNotMember => UnprocessableEntity(Problem("Invalid leader", "The leader must be included in the team membership.", 422)),
                _ => UnprocessableEntity(Problem("Invalid team", "Select at least one valid approved Student.", 422))
            };

    private static ProblemDetails Problem(string title, string detail, int status) => new() { Title = title, Detail = detail, Status = status };
}
