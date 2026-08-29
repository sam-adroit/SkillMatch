using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.DTOs.Workflows;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize]
public sealed class ApplicationsController(IWorkflowService workflows) : ControllerBase
{
    [Authorize(Roles = "Student")]
    [HttpPost("api/projects/{projectId:guid}/applications")]
    public async Task<IActionResult> Apply(Guid projectId, ApplyToProjectRequest request, CancellationToken token) =>
        Map(await workflows.ApplyAsync(CurrentUserId(), projectId, request, token), StatusCodes.Status201Created);

    [Authorize(Roles = "Student")]
    [HttpGet("api/applications")]
    public Task<IReadOnlyList<ApplicationResponse>> Mine(CancellationToken token) =>
        workflows.GetStudentApplicationsAsync(CurrentUserId(), token);

    [Authorize(Roles = "Admin")]
    [HttpGet("api/admin/applications")]
    public Task<IReadOnlyList<ApplicationResponse>> All([FromQuery] ApplicationQuery query, CancellationToken token) =>
        workflows.GetApplicationsAsync(query, token);

    [Authorize(Roles = "Admin")]
    [HttpPatch("api/admin/applications/{id:guid}/decision")]
    public async Task<IActionResult> Decide(Guid id, DecideApplicationRequest request, CancellationToken token) =>
        Map(await workflows.DecideAsync(id, request, token));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult Map<T>(WorkflowResult<T> result, int successStatus = StatusCodes.Status200OK) =>
        result.Failure == WorkflowFailure.None
            ? StatusCode(successStatus, result.Value)
            : WorkflowProblem(result.Failure);

    private IActionResult WorkflowProblem(WorkflowFailure failure) => failure switch
    {
        WorkflowFailure.NotFound => NotFound(),
        WorkflowFailure.MissingProfile => UnprocessableEntity(Problem("Profile required", "Complete and save your Student profile before applying.", 422)),
        WorkflowFailure.DuplicateApplication => Conflict(Problem("Duplicate application", "You have already applied to this project.", 409)),
        WorkflowFailure.ProjectClosed => Conflict(Problem("Project unavailable", "The project must be published and open for this action.", 409)),
        WorkflowFailure.CapacityReached => Conflict(Problem("Capacity reached", "The project's maximum approved or team capacity has been reached.", 409)),
        WorkflowFailure.ExistingAssignment => Conflict(Problem("Existing team assignment", "This Student already belongs to an active team in the current course cycle.", 409)),
        WorkflowFailure.AssignmentLocked => Conflict(Problem("Application is assigned", "Remove the Student from the team before changing this approved application.", 409)),
        _ => UnprocessableEntity(Problem("Invalid workflow request", FailureMessage(failure), 422))
    };

    private static string FailureMessage(WorkflowFailure failure) => failure switch
    {
        WorkflowFailure.InvalidDecision => "Choose Approved, Rejected, or Waitlisted.",
        _ => "The application request is invalid."
    };

    private static ProblemDetails Problem(string title, string detail, int status) => new() { Title = title, Detail = detail, Status = status };
}
