using Core.Domain;

namespace Application.Abstractions;

public interface IEspnDataService
{
    Task<EspnData?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<EspnData>> GetForUserAsync(int userId, CancellationToken ct);
    Task<EspnData?> GetOneAsync(int userId, int seasonId, int leagueId, CancellationToken ct);
    Task<EspnData> UpsertAsync(int userId, int seasonId, int leagueId, string espnS2, string swid, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
