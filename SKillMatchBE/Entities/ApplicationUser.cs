namespace SkillMatchBE.Entities;

public sealed class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Email { get; set; }

    public required string NormalizedEmail { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Student;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public StudentProfile? StudentProfile { get; set; }

    public ICollection<ProjectApplication> ProjectApplications { get; set; } = [];

    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
}
