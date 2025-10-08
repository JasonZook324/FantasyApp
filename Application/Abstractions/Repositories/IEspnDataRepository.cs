using Core.Domain;

namespace Application.Abstractions.Repositories;

public interface IEspnDataRepository
{
    Task<EspnData?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<EspnData>> GetForUserAsync(int userId, CancellationToken ct);
    Task<EspnData?> GetOneAsync(int userId, int seasonId, int leagueId, CancellationToken ct);
    Task<EspnData> AddAsync(EspnData entity, CancellationToken ct);
    Task UpdateAsync(EspnData entity, CancellationToken ct);
    Task RemoveAsync(EspnData entity, CancellationToken ct);
}