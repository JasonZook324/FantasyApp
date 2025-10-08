using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages;

public record AuthUserResponse(int Id, string Username, bool IsActive, int RoleId);

public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LoginModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [FromRoute]
    public string? Mode { get; set; }

    [FromQuery]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string RegisterUsername { get; set; } = string.Empty;

    [BindProperty]
    public string RegisterPassword { get; set; } = string.Empty;

    public string? Error { get; set; }
    public string? RegisterError { get; set; }

    public void OnGet()
    {
        // no-op
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "Username and Password are required.";
            return Page();
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.PostAsJsonAsync("api/auth/login", new { Username, Password }, ct);

            if (res.IsSuccessStatusCode)
            {
                var user = await res.Content.ReadFromJsonAsync<AuthUserResponse>(cancellationToken: ct);
                if (user is null)
                {
                    Error = "Unexpected response from server.";
                    return Page();
                }

                // Store minimal user info in session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("RoleId", user.RoleId);
                HttpContext.Session.SetString("IsActive", user.IsActive ? "true" : "false");

                // Fetch league data for this user and store in session
                try
                {
                    var leagueRes = await client.GetAsync($"api/espndata/espn/league/user/{user.Id}", ct);
                    if (leagueRes.IsSuccessStatusCode)
                    {
                        var json = await leagueRes.Content.ReadAsStringAsync(ct);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var leagueName = root.TryGetProperty("leagueName", out var ln) && ln.ValueKind == JsonValueKind.String
                            ? ln.GetString() : null;
                        if (!string.IsNullOrEmpty(leagueName))
                            HttpContext.Session.SetString("leagueName", leagueName);
                        if (root.TryGetProperty("scoringPeriodId", out var sp) && sp.ValueKind == JsonValueKind.Number)
                            HttpContext.Session.SetInt32("scoringPeriodId", sp.GetInt32());
                        if (root.TryGetProperty("leagueSize", out var ls) && ls.ValueKind == JsonValueKind.Number)
                            HttpContext.Session.SetInt32("leagueSize", ls.GetInt32());
                    }
                }
                catch { }

                // Redirect to returnUrl if local and provided
                if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }

                return RedirectToPage("/Index");
            }

            if (res.StatusCode is HttpStatusCode.Unauthorized)
            {
                Error = "Invalid username or password.";
                return Page();
            }

            Error = $"Login failed ({(int)res.StatusCode}).";
            return Page();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRegisterAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(RegisterUsername) || string.IsNullOrWhiteSpace(RegisterPassword))
        {
            RegisterError = "Username and Password are required.";
            Mode = "register";
            return Page();
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var res = await client.PostAsJsonAsync("api/auth/register", new { Username = RegisterUsername, Password = RegisterPassword, RoleId = 1 }, ct);

            if (res.IsSuccessStatusCode)
            {
                // After register, auto-login
                Username = RegisterUsername;
                Password = RegisterPassword;
                return await OnPostAsync(ct);
            }

            RegisterError = await res.Content.ReadAsStringAsync(ct);
            Mode = "register";
            return Page();
        }
        catch (Exception ex)
        {
            RegisterError = ex.Message;
            Mode = "register";
            return Page();
        }
    }

    // POST /Login?handler=Logout
    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Index");
    }
}