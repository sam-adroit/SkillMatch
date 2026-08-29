namespace SkillMatchBE.Entities;

public sealed class RecommendationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;
    public RecommendationType Type { get; set; } = RecommendationType.Project;
    public Guid TargetId { get; set; }
    public decimal Score { get; set; }
    public required string Explanation { get; set; }
    public required string Provider { get; set; }
    public required string Model { get; set; }
    public RecommendationProviderStatus ProviderStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
