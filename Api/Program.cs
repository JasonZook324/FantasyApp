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

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Pipeline
app.UseHttpsRedirection();

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
