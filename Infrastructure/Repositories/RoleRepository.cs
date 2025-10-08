using Application.Abstractions.Repositories;
using Core.Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly FantasyDbContext _db;
    public RoleRepository(FantasyDbContext db) => _db = db;

    public Task<bool> ExistsAsync(int roleId, CancellationToken ct) => _db.Roles.AnyAsync(r => r.Id == roleId, ct);

    public Task<bool> NameExistsAsync(string name, CancellationToken ct) => _db.Roles.AnyAsync(r => r.Name == name, ct);

    public Task<Role?> GetByIdAsync(int roleId, CancellationToken ct) => _db.Roles.FindAsync(new object?[] { roleId }, ct).AsTask();

    public async Task<Role> AddAsync(Role role, CancellationToken ct)
    {
        await _db.Roles.AddAsync(role, ct);
        await _db.SaveChangesAsync(ct);
        return role;
    }
}
