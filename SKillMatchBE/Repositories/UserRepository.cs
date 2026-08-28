using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillMatchBE.Data;
using SkillMatchBE.Entities;

namespace SkillMatchBE.Repositories;

public sealed class UserRepository(SkillMatchDbContext database) : IUserRepository
{
    public Task<ApplicationUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        database.Users.SingleOrDefaultAsync(
            user => user.Id == id && user.IsActive,
            cancellationToken);

    public Task<ApplicationUser?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        database.Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail && user.IsActive,
            cancellationToken);

    public async Task<bool> TryAddAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        database.Users.Add(user);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            database.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        database.Users.Update(user);
        await database.SaveChangesAsync(cancellationToken);
    }
}
