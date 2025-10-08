using Application.Abstractions;
using Application.Abstractions.Repositories;
using Core.Domain;

namespace Application.Services;

public class EspnDataService : IEspnDataService
{
    private readonly IEspnDataRepository _repo;

    public EspnDataService(IEspnDataRepository repo)
    {
        _repo = repo;
    }

    public Task<EspnData?> GetByIdAsync(int id, CancellationToken ct) => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<EspnData>> GetForUserAsync(int userId, CancellationToken ct) => _repo.GetForUserAsync(userId, ct);

    public Task<EspnData?> GetOneAsync(int userId, int seasonId, int leagueId, CancellationToken ct) => _repo.GetOneAsync(userId, seasonId, leagueId, ct);

    public async Task<EspnData> UpsertAsync(int userId, int seasonId, int leagueId, string espnS2, string swid, CancellationToken ct)
    {
        var existing = await _repo.GetOneAsync(userId, seasonId, leagueId, ct);
        if (existing is null)
        {
            var entity = new EspnData
            {
                UserId = userId,
                EspnS2 = espnS2,
                SWID = swid,
                LeagueId = leagueId,
                SeasonId = seasonId
            };
            return await _repo.AddAsync(entity, ct);
        }
        else
        {
            existing.EspnS2 = espnS2;
            existing.SWID = swid;
            await _repo.UpdateAsync(existing, ct);
            return existing;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return false;
        await _repo.RemoveAsync(entity, ct);
        return true;
    }
}
