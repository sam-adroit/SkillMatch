using SkillMatchBE.DTOs.Workflows;

namespace SkillMatchBE.Services;

public interface IWorkflowService
{
    Task<WorkflowResult<ApplicationResponse>> ApplyAsync(Guid studentId, Guid projectId, ApplyToProjectRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationResponse>> GetStudentApplicationsAsync(Guid studentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationResponse>> GetApplicationsAsync(ApplicationQuery query, CancellationToken cancellationToken);
    Task<WorkflowResult<ApplicationResponse>> DecideAsync(Guid applicationId, DecideApplicationRequest request, CancellationToken cancellationToken);
    Task<WorkflowResult<TeamResponse>> CreateTeamAsync(SaveTeamRequest request, CancellationToken cancellationToken);
    Task<WorkflowResult<TeamResponse>> UpdateTeamAsync(Guid teamId, UpdateTeamRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamResponse>> GetTeamsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamResponse>> GetStudentTeamsAsync(Guid studentId, CancellationToken cancellationToken);
    Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);
}

public enum WorkflowFailure
{
    None,
    NotFound,
    MissingProfile,
    DuplicateApplication,
    ProjectClosed,
    InvalidDecision,
    CapacityReached,
    ExistingAssignment,
    ApplicationNotApproved,
    TeamExists,
    InvalidMembers,
    LeaderNotMember,
    AssignmentLocked
}

public sealed record WorkflowResult<T>(T? Value, WorkflowFailure Failure)
{
    public static WorkflowResult<T> Success(T value) => new(value, WorkflowFailure.None);
    public static WorkflowResult<T> Failed(WorkflowFailure failure) => new(default, failure);
}
