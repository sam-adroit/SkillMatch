using SkillMatchBE.Entities;

namespace SkillMatchBE.Auth;

public interface IJwtTokenService
{
    IssuedToken CreateToken(ApplicationUser user);
}

public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);
