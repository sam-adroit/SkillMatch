using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.DTOs.Auth;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/auth-check")]
public sealed class AdminAccessController(IAuthService authService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AdminAccessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return user is null
            ? Unauthorized()
            : Ok(new AdminAccessResponse("Admin authorization confirmed.", user));
    }
}
