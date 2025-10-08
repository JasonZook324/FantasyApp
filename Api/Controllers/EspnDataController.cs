using System.Net.Http.Headers;
using System.Text.Json;
using Application.Abstractions;
using Application.Abstractions.Logging;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EspnDataController : ControllerBase
{
    private readonly IEspnDataService _svc;
    private readonly ILogService _logs;
    private readonly IHttpClientFactory _httpClientFactory;

    public EspnDataController(IEspnDataService svc, ILogService logs, IHttpClientFactory httpClientFactory)
    {
        _svc = svc;
        _logs = logs;
        _httpClientFactory = httpClientFactory;
    }

    // GET api/espndata/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Core.Domain.EspnData>> GetById(int id, CancellationToken ct)
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
    public async Task<ActionResult<List<Core.Domain.EspnData>>> GetForUser(int userId, CancellationToken ct)
    {
        var list = await _svc.GetForUserAsync(userId, ct);
        await _logs.LogAsync("Info", "EspnData GetForUser: success", "EspnData", userId, null, new { Count = list.Count }, ct);
        return Ok(list);
    }

    // POST api/espndata/upsert
    [HttpPost("upsert")]
    public async Task<ActionResult<Core.Domain.EspnData>> Upsert([FromBody] UpsertEspnDataRequest req, CancellationToken ct)
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

    // GET api/espndata/espn/league/user/{userId}
    [HttpGet("espn/league/user/{userId:int}")]
    public async Task<IActionResult> GetLeagueDataByUser(int userId, CancellationToken ct)
    {
        var data = (await _svc.GetForUserAsync(userId, ct)).FirstOrDefault();
        if (data is null)
        {
            await _logs.LogAsync("Warning", "GetLeagueData: no ESPN data for user", "EspnData", userId, null, null, ct);
            return NotFound("No ESPN data found for user.");
        }

        var client = _httpClientFactory.CreateClient("EspnApi");
        var path = $"seasons/{data.SeasonId}/segments/0/leagues/{data.LeagueId}";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Add("Cookie", $"espn_s2={data.EspnS2}; SWID={data.SWID}");

            var res = await client.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                await _logs.LogAsync("Warning", "GetLeagueData: ESPN call failed", "EspnData", userId, null, new { status = (int)res.StatusCode }, ct);
                return StatusCode((int)res.StatusCode, body);
            }

            string? leagueName = null;
            int? scoringPeriodId = null;
            int? leagueSize = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("settings", out var settings))
                {
                    if (settings.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                        leagueName = nameProp.GetString();
                }
                if (root.TryGetProperty("scoringPeriodId", out var spId) && spId.ValueKind == JsonValueKind.Number)
                    scoringPeriodId = spId.GetInt32();
                if (root.TryGetProperty("teams", out var teams) && teams.ValueKind == JsonValueKind.Array)
                    leagueSize = teams.GetArrayLength();
            }
            catch
            {
                // ignore parse errors
            }

            // Store in session
            HttpContext.Session.SetString("leagueName", leagueName ?? string.Empty);
            if (scoringPeriodId.HasValue)
                HttpContext.Session.SetInt32("scoringPeriodId", scoringPeriodId.Value);
            if (leagueSize.HasValue)
                HttpContext.Session.SetInt32("leagueSize", leagueSize.Value);

            await _logs.LogAsync("Info", "GetLeagueData: success", "EspnData", userId, null,
                new { leagueName, scoringPeriodId, leagueSize, data.LeagueId, data.SeasonId }, ct);

            return Ok(new { leagueName, scoringPeriodId, leagueSize });
        }
        catch (Exception ex)
        {
            await _logs.LogAsync("Error", "GetLeagueData: exception", "EspnData", userId, ex, new { data.LeagueId, data.SeasonId }, ct);
            return StatusCode(500, "Failed to call ESPN API");
        }
    }
}

public record UpsertEspnDataRequest(int UserId, string EspnS2, string SWID, int LeagueId, int SeasonId);
