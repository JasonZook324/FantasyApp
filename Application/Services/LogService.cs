using System.Text.Json;
using Application.Abstractions.Logging;
using Application.Abstractions.Repositories;
using Core.Domain;

namespace Application.Services;

public class LogService : ILogService
{
    private readonly ILogRepository _repo;

    public LogService(ILogRepository repo)
    {
        _repo = repo;
    }

    public async Task<LogEntry> LogAsync(string level, string message, string? category, int? userId, Exception? ex, object? properties, CancellationToken ct)
    {
        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            Category = category,
            UserId = userId,
            Exception = ex?.ToString(),
            PropertiesJson = properties is null ? null : JsonSerializer.Serialize(properties),
            TimestampUtc = DateTimeOffset.UtcNow
        };
        return await _repo.AddAsync(entry, ct);
    }

    public Task<IReadOnlyList<LogEntry>> GetRecentAsync(int take, CancellationToken ct) => _repo.GetRecentAsync(take, ct);
}
