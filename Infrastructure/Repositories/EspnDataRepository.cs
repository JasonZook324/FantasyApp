using Application.Abstractions.Repositories;
using Core.Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EspnDataRepository : IEspnDataRepository
{
    private readonly FantasyDbContext _db;
    public EspnDataRepository(FantasyDbContext db) => _db = db;

    public Task<EspnData?> GetByIdAsync(int id, CancellationToken ct) => _db.EspnDatas.FindAsync(new object?[] { id }, ct).AsTask();

    public async Task<IReadOnlyList<EspnData>> GetForUserAsync(int userId, CancellationToken ct) =>
        await _db.EspnDatas.Where(e => e.UserId == userId)
            .OrderByDescending(e => e.SeasonId)
            .ThenBy(e => e.LeagueId)
            .ToListAsync(ct);

    public Task<EspnData?> GetOneAsync(int userId, int seasonId, int leagueId, CancellationToken ct) =>
        _db.EspnDatas.FirstOrDefaultAsync(e => e.UserId == userId && e.SeasonId == seasonId && e.LeagueId == leagueId, ct);

    public async Task<EspnData> AddAsync(EspnData entity, CancellationToken ct)
    {
        await _db.EspnDatas.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EspnData entity, CancellationToken ct)
    {
        _db.EspnDatas.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(EspnData entity, CancellationToken ct)
    {
        _db.EspnDatas.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
