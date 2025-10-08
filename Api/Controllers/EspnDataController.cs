using Application.Abstractions;
using Application.Abstractions.Logging;
using Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EspnDataController : ControllerBase
{
    private readonly IEspnDataService _svc;
    private readonly ILogService _logs;

    public EspnDataController(IEspnDataService svc, ILogService logs)
    {
        _svc = svc;
        _logs = logs;
    }

    // GET api/espndata/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EspnData>> GetById(int id, CancellationToken ct)
    {
        var entity = await _svc.GetByIdAsync(id, ct);
        if (entity is null)
        {
            await _logs.LogAsync("Warning", "EspnData GetById: not found", "EspnData", id, null, null, ct);
            return NotFound();
        }
        await _logs.LogAsync("Info", "EspnData GetById: success", "EspnData", entity.UserId, null, new { entity.Id }, ct);
        return Ok(entity);
    }

    // GET api/espndata/user/{userId}
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<EspnData>>> GetForUser(int userId, CancellationToken ct)
    {
        var list = await _svc.GetForUserAsync(userId, ct);
        await _logs.LogAsync("Info", "EspnData GetForUser: success", "EspnData", userId, null, new { Count = list.Count }, ct);
        return Ok(list);
    }

    // GET api/espndata
    [HttpGet]
    public async Task<ActionResult<EspnData?>> GetOne([FromQuery] int userId, [FromQuery] int seasonId, [FromQuery] int leagueId, CancellationToken ct)
    {
        var entity = await _svc.GetOneAsync(userId, seasonId, leagueId, ct);
        if (entity is null)
        {
            await _logs.LogAsync("Warning", "EspnData GetOne: not found", "EspnData", userId, null, new { seasonId, leagueId }, ct);
            return NotFound();
        }
        await _logs.LogAsync("Info", "EspnData GetOne: success", "EspnData", userId, null, new { seasonId, leagueId, entityId = entity.Id }, ct);
        return Ok(entity);
    }

    // POST api/espndata/upsert
    [HttpPost("upsert")]
    public async Task<ActionResult<EspnData>> Upsert([FromBody] UpsertEspnDataRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.EspnS2) || req.EspnS2.Length > 500)
        {
            await _logs.LogAsync("Warning", "EspnData Upsert: invalid espn_s2", "EspnData", req.UserId, null, null, ct);
            return BadRequest("espn_s2 is required and must be <= 500 characters.");
        }
        if (string.IsNullOrWhiteSpace(req.SWID) || req.SWID.Length > 100)
        {
            await _logs.LogAsync("Warning", "EspnData Upsert: invalid SWID", "EspnData", req.UserId, null, null, ct);
            return BadRequest("SWID is required and must be <= 100 characters.");
        }
        if (req.SeasonId is < 1000 or > 9999)
        {
            await _logs.LogAsync("Warning", "EspnData Upsert: invalid seasonId", "EspnData", req.UserId, null, new { req.SeasonId }, ct);
            return BadRequest("SeasonId must be a 4-digit year.");
        }

        var entity = await _svc.UpsertAsync(req.UserId, req.SeasonId, req.LeagueId, req.EspnS2, req.SWID, ct);
        if (entity.Id == 0)
        {
            await _logs.LogAsync("Info", "EspnData Upsert: created", "EspnData", req.UserId, null, new { entity.Id }, ct);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }
        await _logs.LogAsync("Info", "EspnData Upsert: updated", "EspnData", req.UserId, null, new { entity.Id }, ct);
        return Ok(entity);
    }

    // DELETE api/espndata/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        if (ok)
        {
            await _logs.LogAsync("Info", "EspnData Delete: success", "EspnData", null, null, new { id }, ct);
            return NoContent();
        }
        await _logs.LogAsync("Warning", "EspnData Delete: not found", "EspnData", null, null, new { id }, ct);
        return NotFound();
    }
}

public record UpsertEspnDataRequest(int UserId, string EspnS2, string SWID, int LeagueId, int SeasonId);
