using Microsoft.AspNetCore.Identity;
using SkillMatchBE.Auth;
using SkillMatchBE.DTOs.Auth;
using SkillMatchBE.Entities;
using SkillMatchBE.Repositories;

namespace SkillMatchBE.Services;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IJwtTokenService tokenService,
    IClock clock) : IAuthService
{
    public async Task<AuthServiceResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);

        if (await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return AuthServiceResult.Failed(AuthFailure.DuplicateEmail);
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = string.Empty,
            Role = UserRole.Student,
            CreatedAt = clock.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        if (!await users.TryAddAsync(user, cancellationToken))
        {
            return AuthServiceResult.Failed(AuthFailure.DuplicateEmail);
        }

        return AuthServiceResult.Success(CreateResponse(user));
    }

    public async Task<AuthServiceResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByNormalizedEmailAsync(
            NormalizeEmail(request.Email),
            cancellationToken);

        if (user is null)
        {
            return AuthServiceResult.Failed(AuthFailure.InvalidCredentials);
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return AuthServiceResult.Failed(AuthFailure.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await users.UpdateAsync(user, cancellationToken);
        }

        return AuthServiceResult.Success(CreateResponse(user));
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null ? null : MapUser(user);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private AuthResponse CreateResponse(ApplicationUser user)
    {
        var token = tokenService.CreateToken(user);
        return new AuthResponse(token.Value, token.ExpiresAt, MapUser(user));
    }

    private static CurrentUserResponse MapUser(ApplicationUser user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.Role.ToString());
}
