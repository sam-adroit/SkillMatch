using SkillMatchBE.DTOs.Recommendations;

namespace SkillMatchBE.Services;

public interface IRecommendationService
{
    Task<RecommendationResult<RecommendationBatchResponse>> RecommendProjectsAsync(Guid studentId, CancellationToken cancellationToken);
    Task<RecommendationResult<IReadOnlyList<RecommendationHistoryResponse>>> GetHistoryAsync(Guid studentId, CancellationToken cancellationToken);
    Task<RecommendationResult<IReadOnlyList<TeammateSuggestionResponse>>> SuggestTeammatesAsync(Guid studentId, CancellationToken cancellationToken);
    Task<RecommendationResult<TeamSkillGapResponse>> GetTeamSkillGapsAsync(Guid teamId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
}

public enum RecommendationFailure
{
    None,
    MissingProfile,
    InsufficientProfile,
    NotFound,
    Forbidden
}

public sealed record RecommendationResult<T>(T? Value, RecommendationFailure Failure, string? Detail = null)
{
    public static RecommendationResult<T> Success(T value) => new(value, RecommendationFailure.None);
    public static RecommendationResult<T> Fail(RecommendationFailure failure, string detail) => new(default, failure, detail);
}
