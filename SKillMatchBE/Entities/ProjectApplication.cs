namespace SkillMatchBE.Entities;

public sealed class ProjectApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public ProjectTopic Project { get; set; } = null!;
    public string Note { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string DecisionNote { get; set; } = string.Empty;
}
