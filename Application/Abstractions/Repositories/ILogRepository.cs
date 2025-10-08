using Core.Domain;

namespace Application.Abstractions.Repositories;

public interface ILogRepository
{
    Task<LogEntry> AddAsync(LogEntry entry, CancellationToken ct);
    Task<IReadOnlyList<LogEntry>> GetRecentAsync(int take, CancellationToken ct);
}
