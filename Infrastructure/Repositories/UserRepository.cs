using Application.Abstractions.Repositories;
using Core.Domain;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly FantasyDbContext _db;
    public UserRepository(FantasyDbContext db) => _db = db;

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct, int? excludeUserId = null)
    {
        var query = _db.Users.AsQueryable();
        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);
        return await query.AnyAsync(u => u.Username == username, ct);
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct) => _db.Users.FindAsync(new object?[] { id }, ct).AsTask();

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct) => _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct) => await _db.Users.OrderBy(u => u.Username).ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(User user, CancellationToken ct)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }
}
