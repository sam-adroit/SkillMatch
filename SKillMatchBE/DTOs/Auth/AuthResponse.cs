namespace SkillMatchBE.DTOs.Auth;

public sealed record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    CurrentUserResponse User);
