namespace SkillMatchBE.Entities;

public sealed class ProjectRequiredSkill
{
    public Guid ProjectId { get; set; }
    public ProjectTopic Project { get; set; } = null!;
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}
