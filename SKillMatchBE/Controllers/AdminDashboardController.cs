using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMatchBE.DTOs.Workflows;
using SkillMatchBE.Services;

namespace SkillMatchBE.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public sealed class AdminDashboardController(IWorkflowService workflows) : ControllerBase
{
    [HttpGet("api/admin/dashboard")]
    public Task<AdminDashboardResponse> Get(CancellationToken token) => workflows.GetDashboardAsync(token);
}
