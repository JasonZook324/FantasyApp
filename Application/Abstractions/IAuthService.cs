using Core.Domain;

namespace Application.Abstractions;

public interface IAuthService
{
    Task<User> RegisterAsync(string username, string password, int roleId, CancellationToken ct);
    Task<User?> LoginAsync(string username, string password, CancellationToken ct);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> UpdateUserAsync(int id, string? username, int? roleId, bool? isActive, CancellationToken ct);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct);
    Task<bool> ChangePasswordAsync(int id, string? currentPassword, string newPassword, CancellationToken ct);
}
