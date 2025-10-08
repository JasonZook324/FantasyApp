using Application.Abstractions;
using Application.Abstractions.Logging;
using Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogService _logs;

    public AuthController(IAuthService auth, ILogService logs)
    {
        _auth = auth;
        _logs = logs;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthUserResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length > 50)
        {
            await _logs.LogAsync("Warning", "Register failed: invalid username", "Auth", null, null, new { req.Username }, ct);
            return BadRequest("Username is required and must be <= 50 characters.");
        }
        if (string.IsNullOrWhiteSpace(req.Password))
        {
            await _logs.LogAsync("Warning", "Register failed: password missing", "Auth", null, null, new { req.Username }, ct);
            return BadRequest("Password is required.");
        }

        try
        {
            var user = await _auth.RegisterAsync(req.Username, req.Password, req.RoleId, ct);
            await _logs.LogAsync("Info", "User registered", "Auth", user.Id, null, new { req.Username, req.RoleId }, ct);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
        }
        catch (InvalidOperationException ex)
        {
            await _logs.LogAsync("Warning", "Register conflict", "Auth", null, ex, new { req.Username, req.RoleId }, ct);
            return Conflict(ex.Message);
        }
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthUserResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        {
            await _logs.LogAsync("Warning", "Login failed: missing credentials", "Auth", null, null, new { req.Username }, ct);
            return BadRequest("Username and Password are required.");
        }

        var user = await _auth.LoginAsync(req.Username, req.Password, ct);
        if (user is null)
        {
            await _logs.LogAsync("Warning", "Login failed: unauthorized", "Auth", null, null, new { req.Username }, ct);
            return Unauthorized();
        }
        await _logs.LogAsync("Info", "Login success", "Auth", user.Id, null, new { req.Username }, ct);
        return Ok(new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
    }

    // GET api/auth/user/{id}
    [HttpGet("user/{id:int}")]
    public async Task<ActionResult<AuthUserResponse>> GetUserById(int id, CancellationToken ct)
    {
        var user = await _auth.GetByIdAsync(id, ct);
        if (user is null)
        {
            await _logs.LogAsync("Warning", "GetUserById: not found", "Auth", id, null, null, ct);
            return NotFound();
        }
        await _logs.LogAsync("Info", "GetUserById: success", "Auth", user.Id, null, null, ct);
        return Ok(new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
    }

    // PUT api/auth/user/{id}
    [HttpPut("user/{id:int}")]
    public async Task<ActionResult<AuthUserResponse>> UpdateUser(int id, [FromBody] UpdateAuthUserRequest req, CancellationToken ct)
    {
        try
        {
            var user = await _auth.UpdateUserAsync(id, req.Username, req.RoleId, req.IsActive, ct);
            if (user is null)
            {
                await _logs.LogAsync("Warning", "UpdateUser: not found", "Auth", id, null, new { req.Username, req.RoleId, req.IsActive }, ct);
                return NotFound();
            }
            await _logs.LogAsync("Info", "User updated", "Auth", user.Id, null, new { req.Username, req.RoleId, req.IsActive }, ct);
            return Ok(new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
        }
        catch (InvalidOperationException ex)
        {
            await _logs.LogAsync("Warning", "UpdateUser conflict", "Auth", id, ex, new { req.Username, req.RoleId, req.IsActive }, ct);
            return Conflict(ex.Message);
        }
    }

    // DELETE api/auth/user/{id}  (soft delete -> IsActive = false)
    [HttpDelete("user/{id:int}")]
    public async Task<IActionResult> SoftDeleteUser(int id, CancellationToken ct)
    {
        var ok = await _auth.SoftDeleteAsync(id, ct);
        if (ok)
        {
            await _logs.LogAsync("Info", "User soft-deleted", "Auth", id, null, null, ct);
            return NoContent();
        }
        await _logs.LogAsync("Warning", "SoftDelete: not found", "Auth", id, null, null, ct);
        return NotFound();
    }

    // PUT api/auth/user/{id}/password
    [HttpPut("user/{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword))
        {
            await _logs.LogAsync("Warning", "ChangePassword failed: new password missing", "Auth", id, null, null, ct);
            return BadRequest("NewPassword is required.");
        }
        var ok = await _auth.ChangePasswordAsync(id, req.CurrentPassword, req.NewPassword, ct);
        if (ok)
        {
            await _logs.LogAsync("Info", "Password changed", "Auth", id, null, null, ct);
            return NoContent();
        }
        await _logs.LogAsync("Warning", "ChangePassword unauthorized", "Auth", id, null, null, ct);
        return Unauthorized();
    }
}

public record RegisterRequest(string Username, string Password, int RoleId);
public record LoginRequest(string Username, string Password);
public record UpdateAuthUserRequest(string Username, int RoleId, bool IsActive);
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
public record AuthUserResponse(int Id, string? Username, bool? IsActive, int? RoleId);
