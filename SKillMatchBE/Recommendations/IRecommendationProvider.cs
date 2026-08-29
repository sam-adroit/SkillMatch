namespace SkillMatchBE.Recommendations;

public sealed record RecommendationProfileFacts(
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Interests,
    IReadOnlyList<string> PreferredTechnologies);

public sealed record ProjectExplanationInput(
    Guid ProjectId,
    string Title,
    string Category,
    string Difficulty,
    IReadOnlyList<string> RequiredSkills,
    decimal Score,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills);

public sealed record RecommendationExplanationRequest(
    RecommendationProfileFacts Profile,
    IReadOnlyList<ProjectExplanationInput> Projects);

public sealed record RecommendationProviderResult(
    IReadOnlyDictionary<Guid, string> Explanations,
    string Provider,
    string Model);

public interface IRecommendationProvider
{
    Task<RecommendationProviderResult> GenerateProjectExplanationsAsync(
        RecommendationExplanationRequest request,
        CancellationToken cancellationToken);
}
