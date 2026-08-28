using SkillMatchBE.DTOs.Projects;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public interface IProjectRepository
{
    Task<IReadOnlyList<ProjectTopic>> SearchPublishedAsync(ProjectQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectTopic>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProjectTopic?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> NormalizedTitleExistsAsync(string normalizedTitle, Guid? exceptId, CancellationToken cancellationToken);
    Task AddAsync(ProjectTopic project, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task DeleteAsync(ProjectTopic project, CancellationToken cancellationToken);
}
