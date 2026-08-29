using SkillMatchBE.DTOs.Workflows;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public interface IApplicationTeamRepository
{
    Task<T> InSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
    Task<ProjectApplication?> GetApplicationAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectApplication?> GetApplicationAsync(Guid studentId, Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectApplication>> GetStudentApplicationsAsync(Guid studentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectApplication>> GetApplicationsAsync(ApplicationQuery query, CancellationToken cancellationToken);
    Task<int> CountApprovedApplicationsAsync(Guid projectId, Guid? exceptApplicationId, CancellationToken cancellationToken);
    Task AddApplicationAsync(ProjectApplication application, CancellationToken cancellationToken);
    Task<Team?> GetTeamAsync(Guid id, CancellationToken cancellationToken);
    Task<Team?> GetTeamForProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Team>> GetTeamsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Team>> GetStudentTeamsAsync(Guid studentId, CancellationToken cancellationToken);
    Task<bool> HasActiveTeamAsync(Guid studentId, Guid? exceptTeamId, CancellationToken cancellationToken);
    Task<bool> HasApprovedApplicationAsync(Guid studentId, Guid projectId, CancellationToken cancellationToken);
    Task<bool> IsTeamMemberAsync(Guid studentId, Guid teamId, CancellationToken cancellationToken);
    Task AddTeamAsync(Team team, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<DashboardCounts> GetDashboardCountsAsync(CancellationToken cancellationToken);
}

public sealed record DashboardCounts(int Students, int Projects, int Teams, int PendingApplications, int UnassignedStudents);
