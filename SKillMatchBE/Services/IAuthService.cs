using SkillMatchBE.DTOs.Auth;

namespace SkillMatchBE.Services;

public interface IAuthService
{
    Task<AuthServiceResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<AuthServiceResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<CurrentUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public enum AuthFailure
{
    None,
    DuplicateEmail,
    InvalidCredentials
}

public sealed record AuthServiceResult(AuthResponse? Response, AuthFailure Failure)
{
    public bool Succeeded => Failure == AuthFailure.None && Response is not null;

    public static AuthServiceResult Success(AuthResponse response) =>
        new(response, AuthFailure.None);

    public static AuthServiceResult Failed(AuthFailure failure) =>
        new(null, failure);
}
