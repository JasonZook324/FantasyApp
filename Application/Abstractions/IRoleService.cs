using Core.Domain;

namespace Application.Abstractions;

public interface IRoleService
{
    Task<Role> AddAsync(string name, CancellationToken ct);
    Task<bool> ExistsAsync(int roleId, CancellationToken ct);
}
