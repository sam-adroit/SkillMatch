using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Data;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public sealed class RecommendationRepository(SkillMatchDbContext database) : IRecommendationRepository
{
    public Task<StudentProfile?> GetProfileAsync(Guid studentId, CancellationToken cancellationToken) =>
        Profiles().SingleOrDefaultAsync(item => item.UserId == studentId, cancellationToken);

    public async Task<IReadOnlyList<ProjectTopic>> GetPublishedProjectsAsync(CancellationToken cancellationToken) =>
        await database.Projects
            .Include(item => item.Category)
            .Include(item => item.RequiredSkills).ThenInclude(item => item.Skill)
            .Where(item => item.Status == ProjectStatus.Published)
            .OrderBy(item => item.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RecommendationHistory>> GetHistoryAsync(Guid studentId, CancellationToken cancellationToken) =>
        await database.RecommendationHistory
            .Where(item => item.StudentId == studentId && item.Type == RecommendationType.Project)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Score)
            .Take(50)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetProjectTitlesAsync(
        IReadOnlyCollection<Guid> projectIds,
        CancellationToken cancellationToken) =>
        await database.Projects
            .Where(item => projectIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Title, cancellationToken);

    public async Task AddHistoryAsync(IReadOnlyCollection<RecommendationHistory> history, CancellationToken cancellationToken)
    {
        database.RecommendationHistory.AddRange(history);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentProfile>> GetAvailableProfilesAsync(Guid exceptStudentId, CancellationToken cancellationToken) =>
        await Profiles()
            .Where(item => item.UserId != exceptStudentId && item.User.IsActive && item.User.Role == UserRole.Student &&
                !item.User.TeamMemberships.Any(member => member.Team.Status == TeamStatus.Active))
            .OrderBy(item => item.UserId)
            .ToListAsync(cancellationToken);

    public Task<Team?> GetTeamWithProfilesAsync(Guid teamId, CancellationToken cancellationToken) =>
        database.Teams
            .Include(item => item.Project).ThenInclude(item => item.RequiredSkills).ThenInclude(item => item.Skill)
            .Include(item => item.Members).ThenInclude(item => item.Student).ThenInclude(item => item.StudentProfile)
                .ThenInclude(item => item!.Skills).ThenInclude(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == teamId && item.Status == TeamStatus.Active, cancellationToken);

    private IQueryable<StudentProfile> Profiles() => database.StudentProfiles
        .Include(item => item.User)
        .Include(item => item.Skills).ThenInclude(item => item.Skill)
        .Include(item => item.Interests).ThenInclude(item => item.Interest);
}
