using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Data;
using SkillMatchBE.DTOs.Projects;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public sealed class ProjectRepository(SkillMatchDbContext database) : IProjectRepository
{
    public async Task<IReadOnlyList<ProjectTopic>> SearchPublishedAsync(ProjectQuery query, CancellationToken cancellationToken)
    {
        var projects = Included().Where(item => item.Status == ProjectStatus.Published);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            projects = projects.Where(item => EF.Functions.ILike(item.Title, $"%{search}%") || EF.Functions.ILike(item.Description, $"%{search}%"));
        }

        if (query.SkillId is not null)
            projects = projects.Where(item => item.RequiredSkills.Any(skill => skill.SkillId == query.SkillId));
        if (query.CategoryId is not null)
            projects = projects.Where(item => item.CategoryId == query.CategoryId);
        if (Enum.TryParse<ProjectDifficulty>(query.Difficulty, true, out var difficulty))
            projects = projects.Where(item => item.Difficulty == difficulty);
        if (query.TeamSize is not null)
            projects = projects.Where(item => item.MinimumTeamSize <= query.TeamSize && item.MaximumTeamSize >= query.TeamSize);

        return await projects.OrderBy(item => item.Title).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectTopic>> GetAllAsync(CancellationToken cancellationToken) =>
        await Included().OrderBy(item => item.Title).ToListAsync(cancellationToken);

    public Task<ProjectTopic?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Included().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> NormalizedTitleExistsAsync(string normalizedTitle, Guid? exceptId, CancellationToken cancellationToken) =>
        database.Projects.AnyAsync(item => item.NormalizedTitle == normalizedTitle && item.Id != exceptId, cancellationToken);

    public async Task AddAsync(ProjectTopic project, CancellationToken cancellationToken)
    {
        database.Projects.Add(project);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(ProjectTopic project, CancellationToken cancellationToken)
    {
        database.Projects.Remove(project);
        await database.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ProjectTopic> Included() => database.Projects
        .Include(item => item.Category)
        .Include(item => item.RequiredSkills).ThenInclude(item => item.Skill);
}
