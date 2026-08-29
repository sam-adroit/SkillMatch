namespace SkillMatchBE.Entities;

public sealed class ProjectTopic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string NormalizedTitle { get; set; }
    public required string Description { get; set; }
    public string AdminNotes { get; set; } = string.Empty;
    public ProjectDifficulty Difficulty { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public int MinimumTeamSize { get; set; }
    public int PreferredTeamSize { get; set; }
    public int MaximumTeamSize { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ProjectRequiredSkill> RequiredSkills { get; set; } = [];
    public ICollection<ProjectApplication> Applications { get; set; } = [];
    public Team? Team { get; set; }
}
