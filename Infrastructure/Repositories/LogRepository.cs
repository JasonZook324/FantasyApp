using Application.Abstractions.Repositories;
using Core.Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LogRepository : ILogRepository
{
    private readonly FantasyDbContext _db;
    public LogRepository(FantasyDbContext db) => _db = db;

    public async Task<LogEntry> AddAsync(LogEntry entry, CancellationToken ct)
    {
        await _db.Logs.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<IReadOnlyList<LogEntry>> GetRecentAsync(int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 500);
        return await _db.Logs
            .OrderByDescending(l => l.TimestampUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}
