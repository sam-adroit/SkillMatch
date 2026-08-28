using SkillMatchBE.DTOs.Catalog;
using SkillMatchBE.DTOs.Projects;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;

namespace SkillMatchBE.Services;

public sealed class ProjectService(IProjectRepository projects, ILookupRepository lookups, IClock clock) : IProjectService
{
    public async Task<IReadOnlyList<ProjectResponse>> SearchAsync(ProjectQuery query, CancellationToken cancellationToken) =>
        (await projects.SearchPublishedAsync(query, cancellationToken)).Select(MapPublic).ToArray();

    public async Task<ProjectResponse?> GetPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(id, cancellationToken);
        return project?.Status == ProjectStatus.Published ? MapPublic(project) : null;
    }

    public async Task<IReadOnlyList<AdminProjectResponse>> GetAllForAdminAsync(CancellationToken cancellationToken) =>
        (await projects.GetAllAsync(cancellationToken)).Select(MapAdmin).ToArray();

    public Task<ProjectServiceResult> CreateAsync(SaveProjectRequest request, CancellationToken cancellationToken) =>
        SaveAsync(null, request, cancellationToken);

    public async Task<ProjectServiceResult> UpdateAsync(Guid id, SaveProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(id, cancellationToken);
        return project is null
            ? new(null, ProjectFailure.NotFound)
            : await SaveAsync(project, request, cancellationToken);
    }

    public async Task<ProjectServiceResult> ChangeStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(id, cancellationToken);
        if (project is null) return new(null, ProjectFailure.NotFound);
        if (!Enum.TryParse<ProjectStatus>(status, true, out var parsed) || parsed == ProjectStatus.Draft)
            return new(null, ProjectFailure.InvalidStatus);
        if (parsed == ProjectStatus.Published && project.RequiredSkills.Count == 0)
            return new(null, ProjectFailure.MissingRequiredSkills);

        project.Status = parsed;
        project.UpdatedAt = clock.UtcNow;
        await projects.SaveAsync(cancellationToken);
        return new(MapAdmin(project), ProjectFailure.None);
    }

    public async Task<ProjectFailure> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(id, cancellationToken);
        if (project is null) return ProjectFailure.NotFound;
        if (project.Status != ProjectStatus.Draft) return ProjectFailure.DeleteBlocked;
        await projects.DeleteAsync(project, cancellationToken);
        return ProjectFailure.None;
    }

    private async Task<ProjectServiceResult> SaveAsync(ProjectTopic? project, SaveProjectRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProjectDifficulty>(request.Difficulty, true, out var difficulty))
            return new(null, ProjectFailure.InvalidDifficulty);
        if (request.MinimumTeamSize > request.PreferredTeamSize || request.PreferredTeamSize > request.MaximumTeamSize)
            return new(null, ProjectFailure.InvalidTeamSizes);
        var skillIds = request.RequiredSkillIds.Distinct().ToArray();
        if (skillIds.Length == 0) return new(null, ProjectFailure.MissingRequiredSkills);
        if (!await lookups.CategoryExistsAsync(request.CategoryId, cancellationToken) || !await lookups.SkillsExistAsync(skillIds, cancellationToken))
            return new(null, ProjectFailure.InvalidLookup);

        var normalizedTitle = LookupService.Normalize(request.Title);
        if (await projects.NormalizedTitleExistsAsync(normalizedTitle, project?.Id, cancellationToken))
            return new(null, ProjectFailure.DuplicateTitle);

        var now = clock.UtcNow;
        project ??= new ProjectTopic
        {
            Title = string.Empty,
            NormalizedTitle = string.Empty,
            Description = string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };
        project.Title = request.Title.Trim();
        project.NormalizedTitle = normalizedTitle;
        project.Description = request.Description.Trim();
        project.AdminNotes = request.AdminNotes?.Trim() ?? string.Empty;
        project.Difficulty = difficulty;
        project.CategoryId = request.CategoryId;
        project.MinimumTeamSize = request.MinimumTeamSize;
        project.PreferredTeamSize = request.PreferredTeamSize;
        project.MaximumTeamSize = request.MaximumTeamSize;
        project.UpdatedAt = now;
        foreach (var requiredSkill in project.RequiredSkills.Where(item => !skillIds.Contains(item.SkillId)).ToArray())
            project.RequiredSkills.Remove(requiredSkill);
        foreach (var skillId in skillIds.Where(id => project.RequiredSkills.All(item => item.SkillId != id)))
            project.RequiredSkills.Add(new ProjectRequiredSkill { ProjectId = project.Id, SkillId = skillId });

        if (project.Id != Guid.Empty && await projects.GetAsync(project.Id, cancellationToken) is not null)
            await projects.SaveAsync(cancellationToken);
        else
            await projects.AddAsync(project, cancellationToken);

        return new(MapAdmin((await projects.GetAsync(project.Id, cancellationToken))!), ProjectFailure.None);
    }

    private static ProjectResponse MapPublic(ProjectTopic item) => new(
        item.Id, item.Title, item.Description, item.Difficulty.ToString(), item.Status.ToString(),
        item.MinimumTeamSize, item.PreferredTeamSize, item.MaximumTeamSize,
        new LookupResponse(item.CategoryId, item.Category.Name),
        item.RequiredSkills.Select(skill => new LookupResponse(skill.SkillId, skill.Skill.Name)).OrderBy(skill => skill.Name).ToArray(),
        item.CreatedAt, item.UpdatedAt);

    private static AdminProjectResponse MapAdmin(ProjectTopic item) => new(
        item.Id, item.Title, item.Description, item.AdminNotes, item.Difficulty.ToString(), item.Status.ToString(),
        item.MinimumTeamSize, item.PreferredTeamSize, item.MaximumTeamSize,
        new LookupResponse(item.CategoryId, item.Category.Name),
        item.RequiredSkills.Select(skill => new LookupResponse(skill.SkillId, skill.Skill.Name)).OrderBy(skill => skill.Name).ToArray(),
        item.CreatedAt, item.UpdatedAt);
}
