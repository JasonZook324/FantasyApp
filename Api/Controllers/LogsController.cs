using Application.Abstractions.Logging;
using Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logs;

    public LogsController(ILogService logs)
    {
        _logs = logs;
    }

    // GET api/logs?take=100
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LogEntry>>> Get([FromQuery] int take = 100, CancellationToken ct = default)
    {
        var items = await _logs.GetRecentAsync(take, ct);
        return Ok(items);
    }
}
