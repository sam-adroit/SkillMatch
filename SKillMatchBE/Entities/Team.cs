namespace SkillMatchBE.Entities;

public sealed class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public ProjectTopic Project { get; set; } = null!;
    public required string Name { get; set; }
    public Guid LeaderStudentId { get; set; }
    public ApplicationUser LeaderStudent { get; set; } = null!;
    public TeamStatus Status { get; set; } = TeamStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<TeamMember> Members { get; set; } = [];
}
