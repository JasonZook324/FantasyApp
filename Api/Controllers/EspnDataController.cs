using Core.Domain;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EspnDataController : ControllerBase
{
    private readonly FantasyDbContext _db;

    public EspnDataController(FantasyDbContext db)
    {
        _db = db;
    }

    // GET api/espndata/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EspnData>> GetById(int id, CancellationToken ct)
    {
        var entity = await _db.EspnDatas.FindAsync(new object?[] { id }, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    // GET api/espndata/user/{userId}
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<EspnData>>> GetForUser(int userId, CancellationToken ct)
    {
        var list = await _db.EspnDatas
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.SeasonId)
            .ThenBy(e => e.LeagueId)
            .ToListAsync(ct);
        return Ok(list);
    }

    // GET api/espndata
    [HttpGet]
    public async Task<ActionResult<EspnData?>> GetOne([FromQuery] int userId, [FromQuery] int seasonId, [FromQuery] int leagueId, CancellationToken ct)
    {
        var entity = await _db.EspnDatas
            .FirstOrDefaultAsync(e => e.UserId == userId && e.SeasonId == seasonId && e.LeagueId == leagueId, ct);
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

        var existing = await _db.EspnDatas
            .FirstOrDefaultAsync(e => e.UserId == req.UserId && e.SeasonId == req.SeasonId && e.LeagueId == req.LeagueId, ct);

        if (existing is null)
        {
            var entity = new EspnData
            {
                UserId = req.UserId,
                EspnS2 = req.EspnS2,
                SWID = req.SWID,
                LeagueId = req.LeagueId,
                SeasonId = req.SeasonId
            };
            _db.EspnDatas.Add(entity);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }
        else
        {
            existing.EspnS2 = req.EspnS2;
            existing.SWID = req.SWID;
            await _db.SaveChangesAsync(ct);
            return Ok(existing);
        }
    }

    // DELETE api/espndata/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.EspnDatas.FindAsync(new object?[] { id }, ct);
        if (entity is null) return NotFound();
        _db.EspnDatas.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record UpsertEspnDataRequest(int UserId, string EspnS2, string SWID, int LeagueId, int SeasonId);
