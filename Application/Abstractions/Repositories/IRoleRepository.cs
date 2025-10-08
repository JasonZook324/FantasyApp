using Core.Domain;

namespace Application.Abstractions.Repositories;

public interface IRoleRepository
{
    Task<bool> ExistsAsync(int roleId, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    Task<Role?> GetByIdAsync(int roleId, CancellationToken ct);
    Task<Role> AddAsync(Role role, CancellationToken ct);
}