using Core.Domain;

namespace Application.Abstractions;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct);
    Task<User?> UpdateAsync(int id, string username, bool isActive, int roleId, string? newPassword, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
