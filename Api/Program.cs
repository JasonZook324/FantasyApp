using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Core.Domain;
using Application.Abstractions;
using Application.Services;
using Application.Abstractions.Repositories;
using Infrastructure.Repositories;
using Application.Abstractions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Application services (business logic)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEspnDataService, EspnDataService>();
builder.Services.AddScoped<ILogService, LogService>();

// Repositories (data access)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEspnDataRepository, EspnDataRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();

// Password hasher for users
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Configure EF Core with Neon (PostgreSQL) connection string from appsettings
var connectionString = builder.Configuration.GetConnectionString("FantasyAppDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'FantasyAppDb' not found. Set it in appsettings.json or environment variables.");
}

builder.Services.AddDbContext<FantasyDbContext>(options =>
    options.UseNpgsql(connectionString));

// HttpClient for ESPN API
var espnBaseUrl = builder.Configuration["EspnApi_BaseUrl"];
if (string.IsNullOrWhiteSpace(espnBaseUrl))
{
    throw new InvalidOperationException("EspnApi_BaseUrl not configured in appsettings.");
}

builder.Services.AddHttpClient("EspnApi", client =>
{
    client.BaseAddress = new Uri(espnBaseUrl);
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Pipeline
app.UseHttpsRedirection();
app.UseSession();

app.MapControllers();

app.Run();
