using System.ComponentModel.DataAnnotations;

namespace SkillMatchBE.DTOs.Auth;

public sealed record RegisterRequest(
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password);
