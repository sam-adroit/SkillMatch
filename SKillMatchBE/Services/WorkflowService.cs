using SkillMatchBE.DTOs.Workflows;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;

namespace SkillMatchBE.Services;

public sealed class WorkflowService(
    IApplicationTeamRepository workflows,
    IProjectRepository projects,
    IProfileRepository profiles,
    IClock clock) : IWorkflowService
{
    public Task<WorkflowResult<ApplicationResponse>> ApplyAsync(
        Guid studentId,
        Guid projectId,
        ApplyToProjectRequest request,
        CancellationToken cancellationToken) =>
        workflows.InSerializableTransactionAsync(async token =>
        {
            if (await profiles.GetAsync(studentId, token) is null)
                return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.MissingProfile);
            var project = await projects.GetAsync(projectId, token);
            if (project is null) return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.NotFound);
            if (project.Status != ProjectStatus.Published)
                return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.ProjectClosed);
            if (await workflows.GetApplicationAsync(studentId, projectId, token) is not null)
                return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.DuplicateApplication);

            var application = new ProjectApplication
            {
                StudentId = studentId,
                ProjectId = projectId,
                Note = request.Note?.Trim() ?? string.Empty,
                AppliedAt = clock.UtcNow
            };
            await workflows.AddApplicationAsync(application, token);
            return WorkflowResult<ApplicationResponse>.Success(Map((await workflows.GetApplicationAsync(application.Id, token))!));
        }, cancellationToken);

    public async Task<IReadOnlyList<ApplicationResponse>> GetStudentApplicationsAsync(Guid studentId, CancellationToken cancellationToken) =>
        (await workflows.GetStudentApplicationsAsync(studentId, cancellationToken)).Select(Map).ToArray();

    public async Task<IReadOnlyList<ApplicationResponse>> GetApplicationsAsync(ApplicationQuery query, CancellationToken cancellationToken) =>
        (await workflows.GetApplicationsAsync(query, cancellationToken)).Select(Map).ToArray();

    public Task<WorkflowResult<ApplicationResponse>> DecideAsync(
        Guid applicationId,
        DecideApplicationRequest request,
        CancellationToken cancellationToken) =>
        workflows.InSerializableTransactionAsync(async token =>
        {
            if (!Enum.TryParse<ApplicationStatus>(request.Status, true, out var status) || status == ApplicationStatus.Pending)
                return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.InvalidDecision);
            var application = await workflows.GetApplicationAsync(applicationId, token);
            if (application is null) return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.NotFound);

            if (status == ApplicationStatus.Approved && application.Status != ApplicationStatus.Approved)
            {
                if (application.Project.Status != ProjectStatus.Published)
                    return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.ProjectClosed);
                if (await workflows.CountApprovedApplicationsAsync(application.ProjectId, application.Id, token) >= application.Project.MaximumTeamSize)
                    return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.CapacityReached);
                if (await workflows.HasActiveTeamAsync(application.StudentId, null, token))
                    return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.ExistingAssignment);
            }

            if (status != ApplicationStatus.Approved && application.Status == ApplicationStatus.Approved)
            {
                var team = await workflows.GetTeamForProjectAsync(application.ProjectId, token);
                if (team is not null && await workflows.IsTeamMemberAsync(application.StudentId, team.Id, token))
                    return WorkflowResult<ApplicationResponse>.Failed(WorkflowFailure.AssignmentLocked);
            }

            application.Status = status;
            application.DecisionNote = request.DecisionNote?.Trim() ?? string.Empty;
            application.DecidedAt = clock.UtcNow;
            await workflows.SaveAsync(token);
            return WorkflowResult<ApplicationResponse>.Success(Map(application));
        }, cancellationToken);

    public Task<WorkflowResult<TeamResponse>> CreateTeamAsync(SaveTeamRequest request, CancellationToken cancellationToken) =>
        workflows.InSerializableTransactionAsync(async token =>
        {
            var project = await projects.GetAsync(request.ProjectId, token);
            if (project is null) return WorkflowResult<TeamResponse>.Failed(WorkflowFailure.NotFound);
            if (project.Status != ProjectStatus.Published)
                return WorkflowResult<TeamResponse>.Failed(WorkflowFailure.ProjectClosed);
            if (await workflows.GetTeamForProjectAsync(request.ProjectId, token) is not null)
                return WorkflowResult<TeamResponse>.Failed(WorkflowFailure.TeamExists);
            var memberIds = request.MemberStudentIds.Distinct().ToArray();
            var validation = await ValidateMembers(request.ProjectId, null, request.LeaderStudentId, memberIds, project.MaximumTeamSize, token);
            if (validation != WorkflowFailure.None) return WorkflowResult<TeamResponse>.Failed(validation);

            var now = clock.UtcNow;
            var team = new Team
            {
                ProjectId = request.ProjectId,
                Name = request.Name.Trim(),
                LeaderStudentId = request.LeaderStudentId,
                CreatedAt = now,
                UpdatedAt = now
            };
            foreach (var memberId in memberIds)
                team.Members.Add(new TeamMember { TeamId = team.Id, StudentId = memberId, JoinedAt = now });
            await workflows.AddTeamAsync(team, token);
            return WorkflowResult<TeamResponse>.Success(Map((await workflows.GetTeamAsync(team.Id, token))!));
        }, cancellationToken);

    public Task<WorkflowResult<TeamResponse>> UpdateTeamAsync(
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken) =>
        workflows.InSerializableTransactionAsync(async token =>
        {
            var team = await workflows.GetTeamAsync(teamId, token);
            if (team is null) return WorkflowResult<TeamResponse>.Failed(WorkflowFailure.NotFound);
            if (team.Project.Status != ProjectStatus.Published)
                return WorkflowResult<TeamResponse>.Failed(WorkflowFailure.ProjectClosed);
            var memberIds = request.MemberStudentIds.Distinct().ToArray();
            var validation = await ValidateMembers(team.ProjectId, team.Id, request.LeaderStudentId, memberIds, team.Project.MaximumTeamSize, token);
            if (validation != WorkflowFailure.None) return WorkflowResult<TeamResponse>.Failed(validation);

            team.Name = request.Name.Trim();
            team.LeaderStudentId = request.LeaderStudentId;
            team.UpdatedAt = clock.UtcNow;
            foreach (var member in team.Members.Where(item => !memberIds.Contains(item.StudentId)).ToArray())
                team.Members.Remove(member);
            foreach (var memberId in memberIds.Where(id => team.Members.All(item => item.StudentId != id)))
                team.Members.Add(new TeamMember { TeamId = team.Id, StudentId = memberId, JoinedAt = clock.UtcNow });
            await workflows.SaveAsync(token);
            return WorkflowResult<TeamResponse>.Success(Map((await workflows.GetTeamAsync(team.Id, token))!));
        }, cancellationToken);

    public async Task<IReadOnlyList<TeamResponse>> GetTeamsAsync(CancellationToken cancellationToken) =>
        (await workflows.GetTeamsAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<IReadOnlyList<TeamResponse>> GetStudentTeamsAsync(Guid studentId, CancellationToken cancellationToken) =>
        (await workflows.GetStudentTeamsAsync(studentId, cancellationToken)).Select(Map).ToArray();

    public async Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var counts = await workflows.GetDashboardCountsAsync(cancellationToken);
        return new(counts.Students, counts.Projects, counts.Teams, counts.PendingApplications, counts.UnassignedStudents);
    }

    private async Task<WorkflowFailure> ValidateMembers(
        Guid projectId,
        Guid? teamId,
        Guid leaderId,
        IReadOnlyList<Guid> memberIds,
        int maximumSize,
        CancellationToken token)
    {
        if (memberIds.Count == 0 || memberIds.Any(id => id == Guid.Empty)) return WorkflowFailure.InvalidMembers;
        if (memberIds.Count > maximumSize) return WorkflowFailure.CapacityReached;
        if (!memberIds.Contains(leaderId)) return WorkflowFailure.LeaderNotMember;
        foreach (var memberId in memberIds)
        {
            if (!await workflows.HasApprovedApplicationAsync(memberId, projectId, token))
                return WorkflowFailure.ApplicationNotApproved;
            if (await workflows.HasActiveTeamAsync(memberId, teamId, token))
                return WorkflowFailure.ExistingAssignment;
        }
        return WorkflowFailure.None;
    }

    private static ApplicationResponse Map(ProjectApplication item) => new(
        item.Id, item.StudentId, item.Student.Email, item.ProjectId, item.Project.Title,
        item.Note, item.Status.ToString(), item.AppliedAt, item.DecidedAt, item.DecisionNote);

    private static TeamResponse Map(Team item) => new(
        item.Id, item.ProjectId, item.Project.Title, item.Name, item.Status.ToString(), item.Project.MaximumTeamSize,
        item.Members.OrderBy(member => member.Student.Email).Select(member =>
            new TeamMemberResponse(member.StudentId, member.Student.Email, member.StudentId == item.LeaderStudentId, member.JoinedAt)).ToArray(),
        item.CreatedAt, item.UpdatedAt);
}
