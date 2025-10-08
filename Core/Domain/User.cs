namespace Core.Domain;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public bool Active { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }
}
