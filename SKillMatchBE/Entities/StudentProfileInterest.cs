namespace SkillMatchBE.Entities;

public sealed class StudentProfileInterest
{
    public Guid ProfileUserId { get; set; }
    public StudentProfile Profile { get; set; } = null!;
    public Guid InterestId { get; set; }
    public Interest Interest { get; set; } = null!;
}
