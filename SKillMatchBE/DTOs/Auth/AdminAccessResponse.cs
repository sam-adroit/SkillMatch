namespace SkillMatchBE.DTOs.Auth;

public sealed record AdminAccessResponse(
    string Message,
    CurrentUserResponse User);
