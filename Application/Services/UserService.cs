using Application.Abstractions;
using Application.Abstractions.Repositories;
using Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher<User> _hasher;

    public UserService(IUserRepository users, IRoleRepository roles, IPasswordHasher<User> hasher)
    {
        _users = users;
        _roles = roles;
        _hasher = hasher;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct) => _users.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct) => _users.GetAllAsync(ct);

    public async Task<User?> UpdateAsync(int id, string username, bool isActive, int roleId, string? newPassword, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null) return null;
        if (!string.Equals(user.Username, username, StringComparison.Ordinal) &&
            await _users.UsernameExistsAsync(username, ct, excludeUserId: id))
            throw new InvalidOperationException("Username already exists.");
        if (!await _roles.ExistsAsync(roleId, ct))
            throw new InvalidOperationException("Invalid RoleId.");
        user.Username = username;
        user.IsActive = isActive;
        user.RoleId = roleId;
        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await _users.UpdateAsync(user, ct);
        return user;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null) return false;
        await _users.RemoveAsync(user, ct);
        return true;
    }
}
