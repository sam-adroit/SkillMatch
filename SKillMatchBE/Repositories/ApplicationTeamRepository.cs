using System.Data;
using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Data;
using SkillMatchBE.DTOs.Workflows;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public sealed class ApplicationTeamRepository(SkillMatchDbContext database) : IApplicationTeamRepository
{
    public Task<T> InSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) =>
        database.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });

    public Task<ProjectApplication?> GetApplicationAsync(Guid id, CancellationToken cancellationToken) =>
        IncludedApplications().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<ProjectApplication?> GetApplicationAsync(Guid studentId, Guid projectId, CancellationToken cancellationToken) =>
        IncludedApplications().SingleOrDefaultAsync(item => item.StudentId == studentId && item.ProjectId == projectId, cancellationToken);

    public async Task<IReadOnlyList<ProjectApplication>> GetStudentApplicationsAsync(Guid studentId, CancellationToken cancellationToken) =>
        await IncludedApplications().Where(item => item.StudentId == studentId).OrderByDescending(item => item.AppliedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectApplication>> GetApplicationsAsync(ApplicationQuery query, CancellationToken cancellationToken)
    {
        var applications = IncludedApplications();
        if (Enum.TryParse<ApplicationStatus>(query.Status, true, out var status))
            applications = applications.Where(item => item.Status == status);
        if (query.ProjectId is not null)
            applications = applications.Where(item => item.ProjectId == query.ProjectId);
        return await applications.OrderByDescending(item => item.AppliedAt).ToListAsync(cancellationToken);
    }

    public Task<int> CountApprovedApplicationsAsync(Guid projectId, Guid? exceptApplicationId, CancellationToken cancellationToken) =>
        database.ProjectApplications.CountAsync(item => item.ProjectId == projectId && item.Status == ApplicationStatus.Approved && item.Id != exceptApplicationId, cancellationToken);

    public async Task AddApplicationAsync(ProjectApplication application, CancellationToken cancellationToken)
    {
        database.ProjectApplications.Add(application);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task<Team?> GetTeamAsync(Guid id, CancellationToken cancellationToken) =>
        IncludedTeams().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Team?> GetTeamForProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        IncludedTeams().SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Status == TeamStatus.Active, cancellationToken);

    public async Task<IReadOnlyList<Team>> GetTeamsAsync(CancellationToken cancellationToken) =>
        await IncludedTeams().OrderBy(item => item.Project.Title).ThenBy(item => item.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Team>> GetStudentTeamsAsync(Guid studentId, CancellationToken cancellationToken) =>
        await IncludedTeams().Where(item => item.Status == TeamStatus.Active && item.Members.Any(member => member.StudentId == studentId))
            .OrderBy(item => item.Project.Title).ToListAsync(cancellationToken);

    public Task<bool> HasActiveTeamAsync(Guid studentId, Guid? exceptTeamId, CancellationToken cancellationToken) =>
        database.TeamMembers.AnyAsync(item => item.StudentId == studentId && item.Team.Status == TeamStatus.Active && item.TeamId != exceptTeamId, cancellationToken);

    public Task<bool> HasApprovedApplicationAsync(Guid studentId, Guid projectId, CancellationToken cancellationToken) =>
        database.ProjectApplications.AnyAsync(item => item.StudentId == studentId && item.ProjectId == projectId && item.Status == ApplicationStatus.Approved, cancellationToken);

    public Task<bool> IsTeamMemberAsync(Guid studentId, Guid teamId, CancellationToken cancellationToken) =>
        database.TeamMembers.AnyAsync(item => item.StudentId == studentId && item.TeamId == teamId, cancellationToken);

    public async Task AddTeamAsync(Team team, CancellationToken cancellationToken)
    {
        database.Teams.Add(team);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);

    public async Task<DashboardCounts> GetDashboardCountsAsync(CancellationToken cancellationToken)
    {
        var students = await database.Users.CountAsync(item => item.IsActive && item.Role == UserRole.Student, cancellationToken);
        var projects = await database.Projects.CountAsync(cancellationToken);
        var teams = await database.Teams.CountAsync(item => item.Status == TeamStatus.Active, cancellationToken);
        var pending = await database.ProjectApplications.CountAsync(item => item.Status == ApplicationStatus.Pending, cancellationToken);
        var unassigned = await database.Users.CountAsync(item => item.IsActive && item.Role == UserRole.Student &&
            !item.TeamMemberships.Any(member => member.Team.Status == TeamStatus.Active), cancellationToken);
        return new(students, projects, teams, pending, unassigned);
    }

    private IQueryable<ProjectApplication> IncludedApplications() => database.ProjectApplications
        .Include(item => item.Student)
        .Include(item => item.Project);

    private IQueryable<Team> IncludedTeams() => database.Teams
        .Include(item => item.Project)
        .Include(item => item.Members).ThenInclude(item => item.Student);
}
