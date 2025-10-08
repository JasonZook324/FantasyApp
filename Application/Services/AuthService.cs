using Application.Abstractions;
using Application.Abstractions.Repositories;
using Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher<User> _hasher;

    public AuthService(IUserRepository users, IRoleRepository roles, IPasswordHasher<User> hasher)
    {
        _users = users;
        _roles = roles;
        _hasher = hasher;
    }

    public async Task<User> RegisterAsync(string username, string password, int roleId, CancellationToken ct)
    {
        if (await _users.UsernameExistsAsync(username, ct))
            throw new InvalidOperationException("Username already exists.");
        if (!await _roles.ExistsAsync(roleId, ct))
            throw new InvalidOperationException("Invalid RoleId.");

        var user = new User { Username = username, PasswordHash = string.Empty, IsActive = true, RoleId = roleId };
        user.PasswordHash = _hasher.HashPassword(user, password);
        await _users.AddAsync(user, ct);
        return user;
    }

    public async Task<User?> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user is null || !user.IsActive) return null;
        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await _users.UpdateAsync(user, ct);
            return user;
        }
        return verify == PasswordVerificationResult.Success ? user : null;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct) => _users.GetByIdAsync(id, ct);

    public async Task<User?> UpdateUserAsync(int id, string? username, int? roleId, bool? isActive, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null) return null;

        var changed = false;

        if (username is not null)
        {
            if (!string.Equals(user.Username, username, StringComparison.Ordinal) &&
                await _users.UsernameExistsAsync(username, ct, excludeUserId: id))
                throw new InvalidOperationException("Username already exists.");
            user.Username = username;
            changed = true;
        }

        if (roleId.HasValue)
        {
            if (!await _roles.ExistsAsync(roleId.Value, ct))
                throw new InvalidOperationException("Invalid RoleId.");
            user.RoleId = roleId.Value;
            changed = true;
        }

        if (isActive.HasValue)
        {
            user.IsActive = isActive.Value;
            changed = true;
        }

        if (changed)
        {
            await _users.UpdateAsync(user, ct);
        }

        return user;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null) return false;
        if (!user.IsActive) return true;
        user.IsActive = false;
        await _users.UpdateAsync(user, ct);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int id, string? currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null || !user.IsActive) return false;
        if (!string.IsNullOrWhiteSpace(currentPassword))
        {
            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verify == PasswordVerificationResult.Failed) return false;
        }
        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await _users.UpdateAsync(user, ct);
        return true;
    }
}
