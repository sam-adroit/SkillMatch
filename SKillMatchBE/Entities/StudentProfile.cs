namespace SkillMatchBE.Entities;

public sealed class StudentProfile
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ExperienceLevel ExperienceLevel { get; set; }
    public required string Goals { get; set; }
    public string[] PreferredTechnologies { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<StudentProfileSkill> Skills { get; set; } = [];
    public ICollection<StudentProfileInterest> Interests { get; set; } = [];
}
