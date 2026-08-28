using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;
using SkillMatchBE.Services;

namespace SkillMatchBE.Data;

public sealed class DemoDataSeeder(
    IUserRepository users,
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
    }
}
