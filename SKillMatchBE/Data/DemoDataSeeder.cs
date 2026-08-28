using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;

namespace SkillMatchBE.Data;

public sealed class DemoDataSeeder(
    IUserRepository users,
    SkillMatchDbContext database,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IClock clock,
    IOptions<DemoSeedOptions> options) : IDemoDataSeeder
{
    private readonly DemoSeedOptions seed = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!seed.Enabled)
        {
            return;
        }

        var email = seed.AdminEmail.Trim();
        var normalizedEmail = AuthService.NormalizeEmail(email);
        var existing = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            if (existing.Role != UserRole.Admin)
            {
                throw new InvalidOperationException(
                    "The configured demo Admin email belongs to a non-Admin account.");
            }

            await SeedCatalogAsync(cancellationToken);
            return;
        }

        var admin = new ApplicationUser
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = string.Empty,
            Role = UserRole.Admin,
            CreatedAt = clock.UtcNow
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, seed.AdminPassword);

        if (!await users.TryAddAsync(admin, cancellationToken))
        {
            throw new InvalidOperationException("Unable to create the configured demo Admin.");
        }

        await SeedCatalogAsync(cancellationToken);
    }

    private async Task SeedCatalogAsync(CancellationToken cancellationToken)
    {
        if (!database.Skills.Any())
        {
            database.Skills.AddRange(
                LookupSkill("C#"), LookupSkill("React"), LookupSkill("PostgreSQL"), LookupSkill("UX Design"));
        }
        if (!database.Interests.Any())
        {
            database.Interests.AddRange(
                LookupInterest("Artificial Intelligence"), LookupInterest("Education"), LookupInterest("Web Applications"));
        }
        if (!database.Categories.Any())
        {
            database.Categories.AddRange(LookupCategory("Education"), LookupCategory("Productivity"));
        }
        await database.SaveChangesAsync(cancellationToken);

        if (!database.Projects.Any())
        {
            var category = database.Categories.OrderBy(item => item.Name).First();
            var skills = database.Skills.OrderBy(item => item.Name).Take(2).ToArray();
            var now = clock.UtcNow;
            var project = new ProjectTopic
            {
                Title = "Campus Collaboration Hub",
                NormalizedTitle = "CAMPUS COLLABORATION HUB",
                Description = "Build a responsive workspace that helps students coordinate project work and share progress.",
                AdminNotes = "Demo project seeded only when demo seeding is enabled.",
                Difficulty = ProjectDifficulty.Intermediate,
                Status = ProjectStatus.Published,
                MinimumTeamSize = 2,
                PreferredTeamSize = 3,
                MaximumTeamSize = 4,
                CategoryId = category.Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            foreach (var skill in skills)
                project.RequiredSkills.Add(new ProjectRequiredSkill { ProjectId = project.Id, SkillId = skill.Id });
            database.Projects.Add(project);
            await database.SaveChangesAsync(cancellationToken);
        }
    }

    private static Skill LookupSkill(string name) => new() { Name = name, NormalizedName = name.ToUpperInvariant() };
    private static Interest LookupInterest(string name) => new() { Name = name, NormalizedName = name.ToUpperInvariant() };
    private static Category LookupCategory(string name) => new() { Name = name, NormalizedName = name.ToUpperInvariant() };
}
