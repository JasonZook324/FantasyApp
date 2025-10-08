using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages;

public class EspnAuthModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EspnAuthModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty] public string EspnS2 { get; set; } = string.Empty;
    [BindProperty] public string Swid { get; set; } = string.Empty;
    [BindProperty] public int LeagueId { get; set; }
    [BindProperty] public int SeasonId { get; set; } = DateTime.UtcNow.Year;
    [BindProperty] public int? UserId { get; set; }

    public bool ShowHelp { get; set; } = true;
    public string? Error { get; set; }
    public string? Success { get; set; }

    // feedback for tests
    public bool? TestOk { get; set; }
    public TestDiagnostics? Diagnostics { get; set; }

    public record TestDiagnostics(
        bool Ok,
        string? BaseUrl,
        string? Url,
        int? StatusCode,
        string? ResponsePreview,
        string? Exception,
        string CookiesSummary,
        string? LeagueName,
        int? ScoringPeriodId,
        int? LeagueSize
    );

    private record EspnDataVm(int Id, int UserId, string EspnS2, string SWID, int LeagueId, int SeasonId);

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Prefill from session if present
        UserId = HttpContext.Session.GetInt32("UserId");
        EspnS2 = HttpContext.Session.GetString("espn_s2") ?? string.Empty;
        Swid = HttpContext.Session.GetString("swid") ?? string.Empty;
        LeagueId = HttpContext.Session.GetInt32("leagueId") ?? 0;
        SeasonId = HttpContext.Session.GetInt32("seasonId") ?? DateTime.UtcNow.Year;
        ShowHelp = HttpContext.Session.GetString("auth_help") != "hidden";

        // If no values in session, try to load existing data for this user from API
        if (UserId is not null)
        {
            try
            {
                var api = _httpClientFactory.CreateClient("Api");
                var list = await api.GetFromJsonAsync<List<EspnDataVm>>($"api/espndata/user/{UserId}", ct);
                var first = list?.FirstOrDefault();
                if (first is not null)
                {
                    EspnS2 = string.IsNullOrWhiteSpace(EspnS2) ? first.EspnS2 : EspnS2;
                    Swid = string.IsNullOrWhiteSpace(Swid) ? first.SWID : Swid;
                    LeagueId = LeagueId <= 0 ? first.LeagueId : LeagueId;
                    SeasonId = SeasonId < 1000 ? first.SeasonId : SeasonId;
                }
            }
            catch { /* ignore prefill failures */ }
        }
    }

    public async Task<IActionResult> OnPostTestAsync(CancellationToken ct)
    {
        Diagnostics = await TestConnectionAsync(EspnS2, Swid, SeasonId, LeagueId, ct);
        TestOk = Diagnostics.Ok;
        if (Diagnostics.Ok)
        {
            // Store league info for banner
            if (!string.IsNullOrWhiteSpace(Diagnostics.LeagueName))
                HttpContext.Session.SetString("leagueName", Diagnostics.LeagueName);
            if (Diagnostics.ScoringPeriodId is int sp)
                HttpContext.Session.SetInt32("scoringPeriodId", sp);
            if (Diagnostics.LeagueSize is int ls)
                HttpContext.Session.SetInt32("leagueSize", ls);
            HttpContext.Session.SetInt32("seasonId", SeasonId);
            Success = "Connection successful.";
            return Page();
        }
        Error = "Failed to connect. Check your cookies, league id and season.";
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        // Test before save
        Diagnostics = await TestConnectionAsync(EspnS2, Swid, SeasonId, LeagueId, ct);
        TestOk = Diagnostics.Ok;
        if (!Diagnostics.Ok)
        {
            Error = "Failed to connect. Please verify cookies and league info before saving.";
            return Page();
        }

        var sessionUserId = HttpContext.Session.GetInt32("UserId");
        var userId = UserId ?? sessionUserId;
        if (userId is null)
        {
            return RedirectToPage("/Login");
        }

        // Save via API upsert with the current user id
        var api = _httpClientFactory.CreateClient("Api");
        var payload = new { UserId = userId.Value, EspnS2 = EspnS2, SWID = Swid, LeagueId = LeagueId, SeasonId = SeasonId };
        var res = await api.PostAsJsonAsync("api/espndata/upsert", payload, ct);
        if (!res.IsSuccessStatusCode)
        {
            Error = $"Save failed ({(int)res.StatusCode}).";
            return Page();
        }

        // Update session values (including league info for banner)
        HttpContext.Session.SetString("espn_s2", EspnS2);
        HttpContext.Session.SetString("swid", Swid);
        HttpContext.Session.SetInt32("leagueId", LeagueId);
        HttpContext.Session.SetInt32("seasonId", SeasonId);
        if (!string.IsNullOrWhiteSpace(Diagnostics.LeagueName))
            HttpContext.Session.SetString("leagueName", Diagnostics.LeagueName);
        if (Diagnostics.ScoringPeriodId is int sp)
            HttpContext.Session.SetInt32("scoringPeriodId", sp);
        if (Diagnostics.LeagueSize is int ls)
            HttpContext.Session.SetInt32("leagueSize", ls);

        Success = "Cookies saved.";
        return Page();
    }

    public async Task<IActionResult> OnPostRefreshAsync(CancellationToken ct)
    {
        var s2 = HttpContext.Session.GetString("espn_s2") ?? EspnS2;
        var sw = HttpContext.Session.GetString("swid") ?? Swid;
        var league = HttpContext.Session.GetInt32("leagueId") ?? LeagueId;
        var season = HttpContext.Session.GetInt32("seasonId") ?? SeasonId;

        var diags = await TestConnectionAsync(s2, sw, season, league, ct);
        if (diags.Ok)
        {
            if (!string.IsNullOrWhiteSpace(diags.LeagueName))
                HttpContext.Session.SetString("leagueName", diags.LeagueName);
            if (diags.ScoringPeriodId is int sp)
                HttpContext.Session.SetInt32("scoringPeriodId", sp);
            if (diags.LeagueSize is int ls)
                HttpContext.Session.SetInt32("leagueSize", ls);
            HttpContext.Session.SetInt32("seasonId", season);
            Success = "League data refreshed.";
        }
        else
        {
            Error = "Refresh failed. Verify ESPN cookies and league info.";
        }
        Diagnostics = diags;
        TestOk = diags.Ok;
        return Page();
    }

    public IActionResult OnPostDisconnect()
    {
        // Clear ESPN-related session keys
        HttpContext.Session.Remove("espn_s2");
        HttpContext.Session.Remove("swid");
        HttpContext.Session.Remove("leagueId");
        HttpContext.Session.Remove("seasonId");
        HttpContext.Session.Remove("leagueName");
        HttpContext.Session.Remove("scoringPeriodId");
        HttpContext.Session.Remove("leagueSize");
        Success = "Disconnected ESPN credentials.";
        return Page();
    }

    private async Task<TestDiagnostics> TestConnectionAsync(string espnS2, string swid, int seasonId, int leagueId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(espnS2) || string.IsNullOrWhiteSpace(swid) || leagueId <= 0 || seasonId < 1000)
        {
            return new TestDiagnostics(false, null, null, null, "Missing inputs (cookies/league/season)", null, CookiesSummary(espnS2, swid), null, null, null);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("EspnDirect");
            var baseUrl = client.BaseAddress?.ToString();
            var path = $"games/ffl/seasons/{seasonId}/segments/0/leagues/{leagueId}?view=mSettings";
            var url = (client.BaseAddress is null) ? path : new Uri(client.BaseAddress, path).ToString();

            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Add("Cookie", $"espn_s2={espnS2}; SWID={swid}");
            var res = await client.SendAsync(req, ct);

            string? preview = null;
            string? leagueName = null;
            int? scoringPeriodId = null;
            int? leagueSize = null;
            try
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    preview = body.Length > 600 ? body[..600] + "…" : body;
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("settings", out var settings) && settings.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                        leagueName = nameProp.GetString();
                    if (root.TryGetProperty("scoringPeriodId", out var sp) && sp.ValueKind == JsonValueKind.Number)
                        scoringPeriodId = sp.GetInt32();
                    if (root.TryGetProperty("teams", out var teams) && teams.ValueKind == JsonValueKind.Array)
                        leagueSize = teams.GetArrayLength();
                }
            }
            catch { /* ignore body parse errors */ }

            return new TestDiagnostics(res.IsSuccessStatusCode, baseUrl, url, (int)res.StatusCode, preview, null, CookiesSummary(espnS2, swid), leagueName, scoringPeriodId, leagueSize);
        }
        catch (Exception ex)
        {
            return new TestDiagnostics(false, null, null, null, null, ex.Message, CookiesSummary(espnS2, swid), null, null, null);
        }
    }

    private static string CookiesSummary(string s2, string swid)
    {
        static string Mask(string v)
        {
            if (string.IsNullOrEmpty(v)) return "<empty>";
            var head = v.Length > 4 ? v[..4] : v;
            var tail = v.Length > 4 ? v[^4..] : "";
            return $"len={v.Length}, {head}…{tail}";
        }
        return $"espn_s2({Mask(s2)}), swid({Mask(swid)})";
    }
}
