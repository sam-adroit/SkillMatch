namespace SkillMatchBE.DTOs.Recommendations;

public sealed record ProjectRecommendationResponse(
    Guid ProjectId,
    string ProjectTitle,
    decimal Score,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    string Explanation,
    string Provider,
    string Model,
    string ProviderStatus,
    DateTimeOffset CreatedAt);

public sealed record RecommendationBatchResponse(
    IReadOnlyList<ProjectRecommendationResponse> Results,
    bool Reused,
    string ProviderStatus);

public sealed record RecommendationHistoryResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    decimal Score,
    string Explanation,
    string Provider,
    string Model,
    string ProviderStatus,
    DateTimeOffset CreatedAt);

public sealed record TeammateSuggestionResponse(
    Guid StudentId,
    string DisplayName,
    decimal Score,
    IReadOnlyList<string> SharedSkills,
    IReadOnlyList<string> ComplementarySkills,
    IReadOnlyList<string> SharedInterests);

public sealed record TeamSkillGapResponse(
    Guid TeamId,
    Guid ProjectId,
    string ProjectTitle,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> CoveredSkills,
    IReadOnlyList<string> MissingSkills);
