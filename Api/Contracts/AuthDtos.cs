namespace Api.Contracts;

public record RegisterRequest(string Username, string Password, int RoleId);
public record LoginRequest(string Username, string Password);
// Nullable fields to allow partial updates
public record UpdateAuthUserRequest(string? Username, int? RoleId, bool? IsActive);
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
public record AuthUserResponse(int Id, string Username, bool IsActive, int RoleId);
