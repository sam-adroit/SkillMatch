using SkillMatchBE.DTOs.Projects;

namespace SkillMatchBE.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>> SearchAsync(ProjectQuery query, CancellationToken cancellationToken);
    Task<ProjectResponse?> GetPublishedAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminProjectResponse>> GetAllForAdminAsync(CancellationToken cancellationToken);
    Task<ProjectServiceResult> CreateAsync(SaveProjectRequest request, CancellationToken cancellationToken);
    Task<ProjectServiceResult> UpdateAsync(Guid id, SaveProjectRequest request, CancellationToken cancellationToken);
    Task<ProjectServiceResult> ChangeStatusAsync(Guid id, string status, CancellationToken cancellationToken);
    Task<ProjectFailure> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public enum ProjectFailure { None, NotFound, DuplicateTitle, InvalidDifficulty, InvalidTeamSizes, InvalidLookup, MissingRequiredSkills, InvalidStatus, DeleteBlocked }
public sealed record ProjectServiceResult(AdminProjectResponse? Project, ProjectFailure Failure);
