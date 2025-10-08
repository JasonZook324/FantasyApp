using Application.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EspnDataController : ControllerBase
{
    private readonly IEspnDataService _svc;

    public EspnDataController(IEspnDataService svc)
    {
        _svc = svc;
    }

    // GET api/espndata/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EspnData>> GetById(int id, CancellationToken ct)
    {
        var entity = await _svc.GetByIdAsync(id, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    // GET api/espndata/user/{userId}
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<EspnData>>> GetForUser(int userId, CancellationToken ct)
    {
        var list = await _svc.GetForUserAsync(userId, ct);
        return Ok(list);
    }

    // GET api/espndata
    [HttpGet]
    public async Task<ActionResult<EspnData?>> GetOne([FromQuery] int userId, [FromQuery] int seasonId, [FromQuery] int leagueId, CancellationToken ct)
    {
        var entity = await _svc.GetOneAsync(userId, seasonId, leagueId, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    // POST api/espndata/upsert
    [HttpPost("upsert")]
    public async Task<ActionResult<EspnData>> Upsert([FromBody] UpsertEspnDataRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.EspnS2) || req.EspnS2.Length > 500)
            return BadRequest("espn_s2 is required and must be <= 500 characters.");
        if (string.IsNullOrWhiteSpace(req.SWID) || req.SWID.Length > 100)
            return BadRequest("SWID is required and must be <= 100 characters.");
        if (req.SeasonId is < 1000 or > 9999)
            return BadRequest("SeasonId must be a 4-digit year.");

        var entity = await _svc.UpsertAsync(req.UserId, req.SeasonId, req.LeagueId, req.EspnS2, req.SWID, ct);
        if (entity.Id == 0)
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        return Ok(entity);
    }

    // DELETE api/espndata/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}

public record UpsertEspnDataRequest(int UserId, string EspnS2, string SWID, int LeagueId, int SeasonId);
