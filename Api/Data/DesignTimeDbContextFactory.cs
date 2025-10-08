using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Api.Data;

// Design-time factory so 'dotnet ef' can create the DbContext for migrations by reading Api/appsettings.json
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FantasyDbContext>
{
    public FantasyDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("FantasyAppDb")
            ?? throw new InvalidOperationException("Connection string 'FantasyAppDb' not found");

        var optionsBuilder = new DbContextOptionsBuilder<FantasyDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FantasyDbContext(optionsBuilder.Options);
    }
}
