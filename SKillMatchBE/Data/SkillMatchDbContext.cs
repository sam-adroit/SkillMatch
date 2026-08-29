using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Data;

public sealed class SkillMatchDbContext(DbContextOptions<SkillMatchDbContext> options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProjectTopic> Projects => Set<ProjectTopic>();
    public DbSet<ProjectApplication> ProjectApplications => Set<ProjectApplication>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<ApplicationUser>();

        user.ToTable("Users");
        user.HasKey(item => item.Id);
        user.Property(item => item.Email).HasMaxLength(254).IsRequired();
        user.Property(item => item.NormalizedEmail).HasMaxLength(254).IsRequired();
        user.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
        user.Property(item => item.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        user.HasIndex(item => item.NormalizedEmail).IsUnique();

        var profile = modelBuilder.Entity<StudentProfile>();
        profile.ToTable("StudentProfiles");
        profile.HasKey(item => item.UserId);
        profile.Property(item => item.ExperienceLevel).HasConversion<string>().HasMaxLength(20);
        profile.Property(item => item.Goals).HasMaxLength(1000).IsRequired();
        profile.Property(item => item.PreferredTechnologies).HasColumnType("text[]");
        profile.HasOne(item => item.User)
            .WithOne(item => item.StudentProfile)
            .HasForeignKey<StudentProfile>(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureLookup(modelBuilder.Entity<Skill>(), "Skills");
        ConfigureLookup(modelBuilder.Entity<Interest>(), "Interests");
        ConfigureLookup(modelBuilder.Entity<Category>(), "Categories");

        var profileSkill = modelBuilder.Entity<StudentProfileSkill>();
        profileSkill.ToTable("StudentProfileSkills");
        profileSkill.HasKey(item => new { item.ProfileUserId, item.SkillId });
        profileSkill.HasOne(item => item.Profile).WithMany(item => item.Skills)
            .HasForeignKey(item => item.ProfileUserId);
        profileSkill.HasOne(item => item.Skill).WithMany().HasForeignKey(item => item.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        var profileInterest = modelBuilder.Entity<StudentProfileInterest>();
        profileInterest.ToTable("StudentProfileInterests");
        profileInterest.HasKey(item => new { item.ProfileUserId, item.InterestId });
        profileInterest.HasOne(item => item.Profile).WithMany(item => item.Interests)
            .HasForeignKey(item => item.ProfileUserId);
        profileInterest.HasOne(item => item.Interest).WithMany().HasForeignKey(item => item.InterestId)
            .OnDelete(DeleteBehavior.Restrict);

        var project = modelBuilder.Entity<ProjectTopic>();
        project.ToTable("Projects");
        project.HasKey(item => item.Id);
        project.Property(item => item.Title).HasMaxLength(160).IsRequired();
        project.Property(item => item.NormalizedTitle).HasMaxLength(160).IsRequired();
        project.Property(item => item.Description).HasMaxLength(4000).IsRequired();
        project.Property(item => item.AdminNotes).HasMaxLength(2000);
        project.Property(item => item.Difficulty).HasConversion<string>().HasMaxLength(20);
        project.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        project.HasIndex(item => item.NormalizedTitle).IsUnique();
        project.HasIndex(item => new { item.Status, item.CategoryId, item.Difficulty });
        project.HasOne(item => item.Category).WithMany().HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        var requiredSkill = modelBuilder.Entity<ProjectRequiredSkill>();
        requiredSkill.ToTable("ProjectRequiredSkills");
        requiredSkill.HasKey(item => new { item.ProjectId, item.SkillId });
        requiredSkill.HasOne(item => item.Project).WithMany(item => item.RequiredSkills)
            .HasForeignKey(item => item.ProjectId);
        requiredSkill.HasOne(item => item.Skill).WithMany().HasForeignKey(item => item.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        var application = modelBuilder.Entity<ProjectApplication>();
        application.ToTable("ProjectApplications");
        application.HasKey(item => item.Id);
        application.Property(item => item.Note).HasMaxLength(1000);
        application.Property(item => item.DecisionNote).HasMaxLength(1000);
        application.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        application.HasIndex(item => new { item.StudentId, item.ProjectId }).IsUnique();
        application.HasIndex(item => new { item.ProjectId, item.Status });
        application.HasIndex(item => new { item.StudentId, item.Status });
        application.HasOne(item => item.Student).WithMany(item => item.ProjectApplications)
            .HasForeignKey(item => item.StudentId).OnDelete(DeleteBehavior.Restrict);
        application.HasOne(item => item.Project).WithMany(item => item.Applications)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);

        var team = modelBuilder.Entity<Team>();
        team.ToTable("Teams");
        team.HasKey(item => item.Id);
        team.Property(item => item.Name).HasMaxLength(120).IsRequired();
        team.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        team.HasIndex(item => item.ProjectId).IsUnique();
        team.HasIndex(item => item.Status);
        team.HasOne(item => item.Project).WithOne(item => item.Team)
            .HasForeignKey<Team>(item => item.ProjectId).OnDelete(DeleteBehavior.Restrict);
        team.HasOne(item => item.LeaderStudent).WithMany()
            .HasForeignKey(item => item.LeaderStudentId).OnDelete(DeleteBehavior.Restrict);

        var teamMember = modelBuilder.Entity<TeamMember>();
        teamMember.ToTable("TeamMembers");
        teamMember.HasKey(item => new { item.TeamId, item.StudentId });
        teamMember.HasIndex(item => item.StudentId);
        teamMember.HasOne(item => item.Team).WithMany(item => item.Members)
            .HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Cascade);
        teamMember.HasOne(item => item.Student).WithMany(item => item.TeamMemberships)
            .HasForeignKey(item => item.StudentId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLookup<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> lookup, string table)
        where T : class
    {
        lookup.ToTable(table);
        lookup.HasKey("Id");
        lookup.Property<string>("Name").HasMaxLength(100).IsRequired();
        lookup.Property<string>("NormalizedName").HasMaxLength(100).IsRequired();
        lookup.HasIndex("NormalizedName").IsUnique();
    }
}
