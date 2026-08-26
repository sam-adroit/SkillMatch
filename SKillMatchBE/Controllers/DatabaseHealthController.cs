using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatchBE.Data;

namespace SkillMatchBE.Controllers;

[ApiController]
[Route("health/database")]
public sealed class DatabaseHealthController(SkillMatchDbContext database) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await database.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? Ok(new { status = "healthy", database = "PostgreSQL" })
                : StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { status = "unhealthy", database = "PostgreSQL" });
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { status = "unhealthy", database = "PostgreSQL" });
        }
    }
}
