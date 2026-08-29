namespace SkillMatchBE.Entities;

public sealed class TeamMember
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;
    public DateTimeOffset JoinedAt { get; set; }
}
