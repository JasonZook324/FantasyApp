using Application.Abstractions;
using Application.Abstractions.Repositories;
using Core.Domain;

namespace Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roles;

    public RoleService(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<Role> AddAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Role name required", nameof(name));
        name = name.Trim();
        if (name.Length > 25) throw new ArgumentException("Role name too long", nameof(name));
        var exists = await _roles.NameExistsAsync(name, ct);
        if (exists) throw new InvalidOperationException("Role already exists.");
        var role = new Role { Name = name };
        return await _roles.AddAsync(role, ct);
    }

    public Task<bool> ExistsAsync(int roleId, CancellationToken ct) => _roles.ExistsAsync(roleId, ct);
}
