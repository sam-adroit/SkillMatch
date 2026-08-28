namespace SkillMatchBE.Entities;

public sealed class StudentProfileSkill
{
    public Guid ProfileUserId { get; set; }
    public StudentProfile Profile { get; set; } = null!;
    public Guid SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}
