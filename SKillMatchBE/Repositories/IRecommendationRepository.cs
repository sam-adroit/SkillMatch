using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public interface IRecommendationRepository
{
    Task<StudentProfile?> GetProfileAsync(Guid studentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectTopic>> GetPublishedProjectsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RecommendationHistory>> GetHistoryAsync(Guid studentId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, string>> GetProjectTitlesAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken);
    Task AddHistoryAsync(IReadOnlyCollection<RecommendationHistory> history, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentProfile>> GetAvailableProfilesAsync(Guid exceptStudentId, CancellationToken cancellationToken);
    Task<Team?> GetTeamWithProfilesAsync(Guid teamId, CancellationToken cancellationToken);
}
