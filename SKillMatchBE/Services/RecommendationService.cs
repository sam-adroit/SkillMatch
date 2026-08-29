using SkillMatchBE.DTOs.Recommendations;
using SkillMatchBE.Entities;
using SkillMatchBE.Recommendations;
using SkillMatchBE.Repositories;

namespace SkillMatchBE.Services;

public sealed class RecommendationService(
    IRecommendationRepository repository,
    IRecommendationProvider provider,
    IClock clock,
    ILogger<RecommendationService> logger) : IRecommendationService
{
    public const decimal RequiredSkillWeight = 50m;
    public const decimal InterestCategoryWeight = 20m;
    public const decimal PreferredTechnologyWeight = 15m;
    public const decimal DifficultyFitWeight = 15m;

    public async Task<RecommendationResult<RecommendationBatchResponse>> RecommendProjectsAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(studentId, cancellationToken);
        if (profile is null)
            return RecommendationResult<RecommendationBatchResponse>.Fail(RecommendationFailure.MissingProfile, "Save your Student profile before requesting recommendations.");
        if (profile.Skills.Count == 0 || profile.Interests.Count == 0 || profile.PreferredTechnologies.Length == 0)
            return RecommendationResult<RecommendationBatchResponse>.Fail(RecommendationFailure.InsufficientProfile, "Add at least one skill, interest, and preferred technology before requesting recommendations.");

        var projects = await repository.GetPublishedProjectsAsync(cancellationToken);
        if (projects.Count == 0)
            return RecommendationResult<RecommendationBatchResponse>.Success(new([], false, "NoResults"));

        var ranked = projects
            .Select(project => ScoreProject(profile, project))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Project.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Project.Id)
            .Take(3)
            .ToArray();

        var history = await repository.GetHistoryAsync(studentId, cancellationToken);
        var cached = TryUseCachedBatch(profile, ranked, history);
        if (cached is not null)
            return RecommendationResult<RecommendationBatchResponse>.Success(cached);

        var providerStatus = RecommendationProviderStatus.AiGenerated;
        RecommendationProviderResult explanations;
        try
        {
            explanations = await provider.GenerateProjectExplanationsAsync(
                new(
                    new(
                        profile.Skills.Select(item => item.Skill.Name).Order().ToArray(),
                        profile.Interests.Select(item => item.Interest.Name).Order().ToArray(),
                        profile.PreferredTechnologies.Order().ToArray()),
                    ranked.Select(ToProviderInput).ToArray()),
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Recommendation explanation provider failed; deterministic fallback will be stored and returned.");
            providerStatus = RecommendationProviderStatus.Fallback;
            explanations = new(
                ranked.ToDictionary(item => item.Project.Id, BuildFallbackExplanation),
                "Deterministic",
                "fallback-v1");
        }

        var createdAt = clock.UtcNow;
        var stored = ranked.Select(item => new RecommendationHistory
        {
            StudentId = studentId,
            Type = RecommendationType.Project,
            TargetId = item.Project.Id,
            Score = item.Score,
            Explanation = explanations.Explanations[item.Project.Id],
            Provider = explanations.Provider,
            Model = explanations.Model,
            ProviderStatus = providerStatus,
            CreatedAt = createdAt
        }).ToArray();
        await repository.AddHistoryAsync(stored, cancellationToken);

        var results = ranked.Select(item => ToResponse(
            item,
            explanations.Explanations[item.Project.Id],
            explanations.Provider,
            explanations.Model,
            providerStatus,
            createdAt)).ToArray();
        return RecommendationResult<RecommendationBatchResponse>.Success(new(results, false, providerStatus.ToString()));
    }

    public async Task<RecommendationResult<IReadOnlyList<RecommendationHistoryResponse>>> GetHistoryAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var history = await repository.GetHistoryAsync(studentId, cancellationToken);
        var titles = await repository.GetProjectTitlesAsync(history.Select(item => item.TargetId).Distinct().ToArray(), cancellationToken);
        var response = history.Select(item => new RecommendationHistoryResponse(
            item.Id,
            item.TargetId,
            titles.GetValueOrDefault(item.TargetId, "Unavailable project"),
            item.Score,
            item.Explanation,
            item.Provider,
            item.Model,
            item.ProviderStatus.ToString(),
            item.CreatedAt)).ToArray();
        return RecommendationResult<IReadOnlyList<RecommendationHistoryResponse>>.Success(response);
    }

    public async Task<RecommendationResult<IReadOnlyList<TeammateSuggestionResponse>>> SuggestTeammatesAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(studentId, cancellationToken);
        if (profile is null)
            return RecommendationResult<IReadOnlyList<TeammateSuggestionResponse>>.Fail(RecommendationFailure.MissingProfile, "Save your Student profile before requesting teammate suggestions.");

        var ownSkills = profile.Skills.Select(item => item.Skill.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ownInterests = profile.Interests.Select(item => item.Interest.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = await repository.GetAvailableProfilesAsync(studentId, cancellationToken);

        var results = candidates
        .Where(candidate => candidate.UserId != studentId && candidate.User.IsActive && candidate.User.Role == UserRole.Student &&
            !candidate.User.TeamMemberships.Any(member => member.Team.Status == TeamStatus.Active))
        .Select(candidate =>
        {
            var candidateSkills = candidate.Skills.Select(item => item.Skill.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidateInterests = candidate.Interests.Select(item => item.Interest.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var sharedSkills = ownSkills.Intersect(candidateSkills, StringComparer.OrdinalIgnoreCase).Order().ToArray();
            var complementarySkills = candidateSkills.Except(ownSkills, StringComparer.OrdinalIgnoreCase).Order().ToArray();
            var sharedInterests = ownInterests.Intersect(candidateInterests, StringComparer.OrdinalIgnoreCase).Order().ToArray();
            var score = Round(
                Ratio(sharedInterests.Length, ownInterests.Count) * 40m +
                Ratio(complementarySkills.Length, candidateSkills.Count) * 40m +
                Ratio(sharedSkills.Length, ownSkills.Count) * 20m);
            return new TeammateSuggestionResponse(
                candidate.UserId,
                $"Student {candidate.UserId.ToString("N")[..8]}",
                score,
                sharedSkills,
                complementarySkills,
                sharedInterests);
        })
        .OrderByDescending(item => item.Score)
        .ThenBy(item => item.StudentId)
        .Take(10)
        .ToArray();

        return RecommendationResult<IReadOnlyList<TeammateSuggestionResponse>>.Success(results);
    }

    public async Task<RecommendationResult<TeamSkillGapResponse>> GetTeamSkillGapsAsync(
        Guid teamId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var team = await repository.GetTeamWithProfilesAsync(teamId, cancellationToken);
        if (team is null)
            return RecommendationResult<TeamSkillGapResponse>.Fail(RecommendationFailure.NotFound, "Team was not found.");
        if (!isAdmin && team.Members.All(member => member.StudentId != userId))
            return RecommendationResult<TeamSkillGapResponse>.Fail(RecommendationFailure.Forbidden, "Students may view skill gaps only for their own team.");

        var required = team.Project.RequiredSkills.Select(item => item.Skill.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
        var coveredSet = team.Members
            .SelectMany(member => member.Student.StudentProfile?.Skills ?? [])
            .Select(item => item.Skill.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var covered = required.Where(coveredSet.Contains).ToArray();
        var missing = required.Where(skill => !coveredSet.Contains(skill)).ToArray();
        return RecommendationResult<TeamSkillGapResponse>.Success(new(
            team.Id, team.ProjectId, team.Project.Title, required, covered, missing));
    }

    private static ScoredProject ScoreProject(StudentProfile profile, ProjectTopic project)
    {
        var profileSkills = profile.Skills.Select(item => item.Skill.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredSkills = project.RequiredSkills.Select(item => item.Skill.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matched = requiredSkills.Intersect(profileSkills, StringComparer.OrdinalIgnoreCase).Order().ToArray();
        var missing = requiredSkills.Except(profileSkills, StringComparer.OrdinalIgnoreCase).Order().ToArray();
        var interests = profile.Interests.Select(item => item.Interest.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var technologies = profile.PreferredTechnologies.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var technologyMatches = requiredSkills.Intersect(technologies, StringComparer.OrdinalIgnoreCase).Count();
        var score =
            Ratio(matched.Length, requiredSkills.Count) * RequiredSkillWeight +
            (interests.Contains(project.Category.Name) ? InterestCategoryWeight : 0m) +
            Ratio(technologyMatches, requiredSkills.Count) * PreferredTechnologyWeight +
            DifficultyFit(profile.ExperienceLevel, project.Difficulty);
        return new(project, Round(score), matched, missing);
    }

    private static decimal DifficultyFit(ExperienceLevel experience, ProjectDifficulty difficulty)
    {
        var distance = Math.Abs((int)experience - (int)difficulty);
        return distance switch { 0 => DifficultyFitWeight, 1 => 8m, _ => 0m };
    }

    private static decimal Ratio(int numerator, int denominator) => denominator == 0 ? 0m : (decimal)numerator / denominator;
    private static decimal Round(decimal score) => decimal.Round(score, 2, MidpointRounding.AwayFromZero);

    private static ProjectExplanationInput ToProviderInput(ScoredProject item) => new(
        item.Project.Id,
        item.Project.Title,
        item.Project.Category.Name,
        item.Project.Difficulty.ToString(),
        item.Project.RequiredSkills.Select(skill => skill.Skill.Name).Order().ToArray(),
        item.Score,
        item.MatchedSkills,
        item.MissingSkills);

    private static string BuildFallbackExplanation(ScoredProject item)
    {
        var matched = item.MatchedSkills.Length == 0
            ? "This project is primarily a growth opportunity based on your current saved skills"
            : $"Your {string.Join(", ", item.MatchedSkills)} experience aligns with this project";
        return item.MissingSkills.Length == 0
            ? $"{matched}, and your profile covers every required skill."
            : $"{matched}. You could grow by pairing with teammates who bring {string.Join(", ", item.MissingSkills)}.";
    }

    private static RecommendationBatchResponse? TryUseCachedBatch(
        StudentProfile profile,
        IReadOnlyList<ScoredProject> ranked,
        IReadOnlyList<RecommendationHistory> history)
    {
        if (history.Count == 0)
            return null;
        var latestTime = history.Max(item => item.CreatedAt);
        var batch = history.Where(item => item.CreatedAt == latestTime).ToArray();
        if (batch.Any(item => item.ProviderStatus != RecommendationProviderStatus.AiGenerated) ||
            batch.Length != ranked.Count || latestTime < profile.UpdatedAt || ranked.Any(item => latestTime < item.Project.UpdatedAt))
            return null;
        var byProject = batch.ToDictionary(item => item.TargetId);
        if (ranked.Any(item => !byProject.ContainsKey(item.Project.Id)))
            return null;

        var results = ranked.Select(item =>
        {
            var stored = byProject[item.Project.Id];
            return ToResponse(item, stored.Explanation, stored.Provider, stored.Model, stored.ProviderStatus, stored.CreatedAt);
        }).ToArray();
        var status = batch.All(item => item.ProviderStatus == RecommendationProviderStatus.AiGenerated)
            ? RecommendationProviderStatus.AiGenerated.ToString()
            : RecommendationProviderStatus.Fallback.ToString();
        return new(results, true, status);
    }

    private static ProjectRecommendationResponse ToResponse(
        ScoredProject item,
        string explanation,
        string provider,
        string model,
        RecommendationProviderStatus status,
        DateTimeOffset createdAt) => new(
            item.Project.Id,
            item.Project.Title,
            item.Score,
            item.MatchedSkills,
            item.MissingSkills,
            explanation,
            provider,
            model,
            status.ToString(),
            createdAt);

    private sealed record ScoredProject(
        ProjectTopic Project,
        decimal Score,
        string[] MatchedSkills,
        string[] MissingSkills);
}
