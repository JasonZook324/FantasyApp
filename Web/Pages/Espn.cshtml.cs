using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages;

public class EspnModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EspnModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [FromQuery]
    public int? UserId { get; set; }

    public string? LeagueName { get; set; }
    public int? ScoringPeriodId { get; set; }
    public int? LeagueSize { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!UserId.HasValue || UserId.Value <= 0) return;
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.GetAsync($"api/espndata/espn/league/user/{UserId}", ct);
            res.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            LeagueName = root.GetProperty("leagueName").GetString();
            if (root.TryGetProperty("scoringPeriodId", out var sp) && sp.ValueKind == JsonValueKind.Number)
                ScoringPeriodId = sp.GetInt32();
            if (root.TryGetProperty("leagueSize", out var ls) && ls.ValueKind == JsonValueKind.Number)
                LeagueSize = ls.GetInt32();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }
}
