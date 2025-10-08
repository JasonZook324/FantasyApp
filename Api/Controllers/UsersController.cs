using Application.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IRoleService _roles;

    public UsersController(IUserService users, IRoleService roles)
    {
        _users = users;
        _roles = roles;
    }

    // GET api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(Map(user));
    }

    // GET api/users
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        return Ok(users.Select(Map).ToList());
    }

    // PUT api/users/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length > 50)
            return BadRequest("Username is required and must be <= 50 characters.");

        try
        {
            var user = await _users.UpdateAsync(id, req.Username, req.IsActive, req.RoleId, req.Password, ct);
            if (user is null) return NotFound();
            return Ok(Map(user));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // DELETE api/users/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _users.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    // POST api/users/roles
    [HttpPost("roles")]
    public async Task<ActionResult<RoleResponse>> AddRole([FromBody] string roleName, CancellationToken ct)
    {
        try
        {
            var role = await _roles.AddAsync(roleName, ct);
            return Ok(new RoleResponse(role.Id, role.Name));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    private static UserResponse Map(User u) => new(u.Id, u.Username, u.IsActive, u.RoleId);
}

public record UpdateUserRequest(string Username, string? Password, bool IsActive, int RoleId);
public record UserResponse(int Id, string Username, bool IsActive, int RoleId);
public record RoleResponse(int Id, string Name);
