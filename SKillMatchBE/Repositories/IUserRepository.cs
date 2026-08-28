using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ApplicationUser?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<bool> TryAddAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken);
}
