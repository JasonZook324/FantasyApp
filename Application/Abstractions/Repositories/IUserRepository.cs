using Core.Domain;

namespace Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct, int? excludeUserId = null);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
    Task RemoveAsync(User user, CancellationToken ct);
}
