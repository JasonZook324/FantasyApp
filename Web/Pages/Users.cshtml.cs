using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web.Pages;

public record UserVm(int Id, string Username, bool IsActive, int RoleId);

public class UsersModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string? Error { get; set; }
    public List<UserVm> Users { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var data = await client.GetFromJsonAsync<List<UserVm>>("api/users", ct);
            if (data is not null)
                Users = data;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }
}
