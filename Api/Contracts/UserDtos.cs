namespace Api.Contracts;

public record UpdateUserRequest(string Username, string? Password, bool IsActive, int RoleId);
public record UserResponse(int Id, string Username, bool IsActive, int RoleId);
public record RoleResponse(int Id, string Name);
