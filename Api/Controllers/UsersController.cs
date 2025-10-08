using Core.Domain;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly FantasyDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(FantasyDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    // GET api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object?[] { id }, ct);
        return user is null ? NotFound() : Ok(Map(user));
    }

    // GET api/users
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await _db.Users
            .OrderBy(u => u.Username)
            .Select(u => new UserResponse(u.Id, u.Username, u.Active, u.RoleId))
            .ToListAsync(ct);

        return Ok(users);
    }

    // POST api/users (register)
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length > 50)
            return BadRequest("Username is required and must be <= 50 characters.");
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Password is required.");

        var usernameTaken = await _db.Users.AnyAsync(u => u.Username == req.Username, ct);
        if (usernameTaken)
            return Conflict("A user with this username already exists.");

        var user = new User
        {
            Username = req.Username,
            PasswordHash = string.Empty, // set after hashing
            Active = req.Active,
            RoleId = req.RoleId
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, Map(user));
    }

    // PUT api/users/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length > 50)
            return BadRequest("Username is required and must be <= 50 characters.");

        var user = await _db.Users.FindAsync(new object?[] { id }, ct);
        if (user is null) return NotFound();

        if (!string.Equals(user.Username, req.Username, StringComparison.Ordinal))
        {
            var usernameTaken = await _db.Users.AnyAsync(u => u.Username == req.Username && u.Id != id, ct);
            if (usernameTaken)
                return Conflict("A user with this username already exists.");
        }

        user.Username = req.Username;
        user.Active = req.Active;
        user.RoleId = req.RoleId;

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(Map(user));
    }

    // DELETE api/users/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object?[] { id }, ct);
        if (user is null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST api/users/roles
    [HttpPost("roles")]
    public async Task<ActionResult<RoleResponse>> AddRole([FromBody] string roleName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("RoleName is required.");

        roleName = roleName.Trim();
        if (roleName.Length > 25)
            return BadRequest("RoleName must be <= 25 characters.");

        var exists = await _db.Roles.AnyAsync(r => r.Name == roleName, ct);
        if (exists)
            return Conflict("A role with this name already exists.");

        var role = new Role { Name = roleName };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);

        return Ok(new RoleResponse(role.Id, role.Name));
    }

    private static UserResponse Map(User u) => new(u.Id, u.Username, u.Active, u.RoleId);
}

public record CreateUserRequest(string Username, string Password, bool Active, int RoleId);
public record UpdateUserRequest(string Username, string? Password, bool Active, int RoleId);
public record UserResponse(int Id, string Username, bool Active, int RoleId);
public record RoleResponse(int Id, string Name);
