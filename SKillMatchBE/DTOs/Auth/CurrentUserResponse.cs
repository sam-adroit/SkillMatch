namespace SkillMatchBE.DTOs.Auth;

public sealed record CurrentUserResponse(Guid Id, string Email, string Role);
