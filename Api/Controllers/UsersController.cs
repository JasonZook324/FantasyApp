using Application.Abstractions;
using Application.Abstractions.Logging;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IRoleService _roles;
    private readonly ILogService _logs;

    public UsersController(IUserService users, IRoleService roles, ILogService logs)
    {
        _users = users;
        _roles = roles;
        _logs = logs;
    }

    // GET api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null)
        {
            await _logs.LogAsync("Warning", "Users GetById: not found", "Users", id, null, null, ct);
            return NotFound();
        }
        await _logs.LogAsync("Info", "Users GetById: success", "Users", user.Id, null, null, ct);
        return Ok(Map(user));
    }

    // GET api/users
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        await _logs.LogAsync("Info", "Users GetAll: success", "Users", null, null, new { Count = users.Count }, ct);
        return Ok(users.Select(Map).ToList());
    }

    // PUT api/users/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length > 50)
        {
            await _logs.LogAsync("Warning", "Users Update: invalid username", "Users", id, null, new { req.Username }, ct);
            return BadRequest("Username is required and must be <= 50 characters.");
        }

        try
        {
            var user = await _users.UpdateAsync(id, req.Username, req.IsActive, req.RoleId, req.Password, ct);
            if (user is null)
            {
                await _logs.LogAsync("Warning", "Users Update: not found", "Users", id, null, new { req.Username, req.IsActive, req.RoleId }, ct);
                return NotFound();
            }
            await _logs.LogAsync("Info", "Users Update: success", "Users", user.Id, null, new { req.Username, req.IsActive, req.RoleId }, ct);
            return Ok(Map(user));
        }
        catch (InvalidOperationException ex)
        {
            await _logs.LogAsync("Warning", "Users Update: conflict", "Users", id, ex, new { req.Username, req.IsActive, req.RoleId }, ct);
            return Conflict(ex.Message);
        }
    }

    // DELETE api/users/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _users.DeleteAsync(id, ct);
        if (ok)
        {
            await _logs.LogAsync("Info", "Users Delete: success", "Users", id, null, null, ct);
            return NoContent();
        }
        await _logs.LogAsync("Warning", "Users Delete: not found", "Users", id, null, null, ct);
        return NotFound();
    }

    // POST api/users/roles
    [HttpPost("roles")]
    public async Task<ActionResult<RoleResponse>> AddRole([FromBody] string roleName, CancellationToken ct)
    {
        try
        {
            var role = await _roles.AddAsync(roleName, ct);
            await _logs.LogAsync("Info", "Roles Add: success", "Users", null, null, new { role.Id, role.Name }, ct);
            return Ok(new RoleResponse(role.Id, role.Name));
        }
        catch (ArgumentException ex)
        {
            await _logs.LogAsync("Warning", "Roles Add: bad request", "Users", null, ex, new { roleName }, ct);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await _logs.LogAsync("Warning", "Roles Add: conflict", "Users", null, ex, new { roleName }, ct);
            return Conflict(ex.Message);
        }
    }

    private static UserResponse Map(Core.Domain.User u) => new(u.Id, u.Username, u.IsActive, u.RoleId);
}

public record UpdateUserRequest(string Username, string? Password, bool IsActive, int RoleId);
public record UserResponse(int Id, string Username, bool IsActive, int RoleId);
public record RoleResponse(int Id, string Name);
