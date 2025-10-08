var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Session for simple login state
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Typed HttpClient to call backend API
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7141/";
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Direct ESPN API client for testing credentials
var espnBaseUrl = builder.Configuration["EspnApi_BaseUrl"] ?? "https://lm-api-reads.fantasy.espn.com/apis/v3/";
builder.Services.AddHttpClient("EspnDirect", client =>
{
    client.BaseAddress = new Uri(espnBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // enable session

// Force login for all non-static, non-login routes
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isStatic = path.StartsWithSegments("/css") || path.StartsWithSegments("/js") ||
                   path.StartsWithSegments("/images") || path.StartsWithSegments("/favicon.ico") ||
                   path.StartsWithSegments("/lib") || path.StartsWithSegments("/assets");

    if (isStatic || path.StartsWithSegments("/Login"))
    {
        await next();
        return;
    }

    if (context.Session.GetInt32("UserId") is null)
    {
        var returnUrl = context.Request.Path + context.Request.QueryString;
        var redirect = $"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        context.Response.Redirect(redirect);
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapRazorPages();

app.Run();
