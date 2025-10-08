using Core.Domain;

namespace Application.Abstractions.Logging;

public interface ILogService
{
    Task<LogEntry> LogAsync(string level, string message, string? category, int? userId, Exception? ex, object? properties, CancellationToken ct);
    Task<IReadOnlyList<LogEntry>> GetRecentAsync(int take, CancellationToken ct);
}
