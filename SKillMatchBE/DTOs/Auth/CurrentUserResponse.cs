namespace SkillMatchBE.DTOs.Auth;

public sealed record CurrentUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role);
