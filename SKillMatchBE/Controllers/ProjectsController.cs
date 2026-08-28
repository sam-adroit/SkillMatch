using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.DTOs.Projects;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize]
public sealed class ProjectsController(IProjectService projects) : ControllerBase
{
    [HttpGet("api/projects")]
    public Task<IReadOnlyList<ProjectResponse>> Search([FromQuery] ProjectQuery query, CancellationToken token) =>
        projects.SearchAsync(query, token);

    [HttpGet("api/projects/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken token)
    {
        var project = await projects.GetPublishedAsync(id, token);
        return project is null ? NotFound() : Ok(project);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("api/admin/projects")]
    public Task<IReadOnlyList<AdminProjectResponse>> GetAll(CancellationToken token) =>
        projects.GetAllForAdminAsync(token);

    [Authorize(Roles = "Admin")]
    [HttpPost("api/admin/projects")]
    public async Task<IActionResult> Create(SaveProjectRequest request, CancellationToken token) =>
        Map(await projects.CreateAsync(request, token), true);

    [Authorize(Roles = "Admin")]
    [HttpPut("api/admin/projects/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveProjectRequest request, CancellationToken token) =>
        Map(await projects.UpdateAsync(id, request, token));

    [Authorize(Roles = "Admin")]
    [HttpPatch("api/admin/projects/{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeProjectStatusRequest request, CancellationToken token) =>
        Map(await projects.ChangeStatusAsync(id, request.Status, token));

    [Authorize(Roles = "Admin")]
    [HttpDelete("api/admin/projects/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        var result = await projects.DeleteAsync(id, token);
        return result switch
        {
            ProjectFailure.None => NoContent(),
            ProjectFailure.NotFound => NotFound(),
            ProjectFailure.DeleteBlocked => Conflict(Problem("Project cannot be deleted", "Only Draft projects can be deleted. Close published projects instead.", 409)),
            _ => BadRequest()
        };
    }

    private IActionResult Map(ProjectServiceResult result, bool created = false) => result.Failure switch
    {
        ProjectFailure.None when created => StatusCode(StatusCodes.Status201Created, result.Project),
        ProjectFailure.None => Ok(result.Project),
        ProjectFailure.NotFound => NotFound(),
        ProjectFailure.DuplicateTitle => Conflict(Problem("Duplicate project title", "A project with this title already exists.", 409)),
        ProjectFailure.DeleteBlocked => Conflict(Problem("Project cannot be deleted", "Only Draft projects can be deleted.", 409)),
        _ => UnprocessableEntity(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Project"] = [FailureMessage(result.Failure)]
        }) { Status = 422, Title = "Project validation failed" })
    };

    private static string FailureMessage(ProjectFailure failure) => failure switch
    {
        ProjectFailure.InvalidDifficulty => "Choose Beginner, Intermediate, or Advanced difficulty.",
        ProjectFailure.InvalidTeamSizes => "Team sizes must satisfy minimum ≤ preferred ≤ maximum.",
        ProjectFailure.InvalidLookup => "The selected category or skill does not exist.",
        ProjectFailure.MissingRequiredSkills => "Select at least one required skill.",
        ProjectFailure.InvalidStatus => "A project can only be Published or Closed through this action.",
        _ => "The project request is invalid."
    };

    private static ProblemDetails Problem(string title, string detail, int status) => new() { Title = title, Detail = detail, Status = status };
}
