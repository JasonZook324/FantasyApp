using Application.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthUserResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length > 50)
            return BadRequest("Username is required and must be <= 50 characters.");
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Password is required.");

        try
        {
            var user = await _auth.RegisterAsync(req.Username, req.Password, req.RoleId, ct);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthUserResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Username and Password are required.");

        var user = await _auth.LoginAsync(req.Username, req.Password, ct);
        if (user is null) return Unauthorized();
        return Ok(new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
    }

    // GET api/auth/user/{id}
    [HttpGet("user/{id:int}")]
    public async Task<ActionResult<AuthUserResponse>> GetUserById(int id, CancellationToken ct)
    {
        var user = await _auth.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
    }

    // PUT api/auth/user/{id}
    [HttpPut("user/{id:int}")]
    public async Task<ActionResult<AuthUserResponse>> UpdateUser(int id, [FromBody] UpdateAuthUserRequest req, CancellationToken ct)
    {
        

        try
        {
            var user = await _auth.UpdateUserAsync(id, req.Username, req.RoleId, req.IsActive, ct);
            if (user is null) return NotFound();
            return Ok(new AuthUserResponse(user.Id, user.Username, user.IsActive, user.RoleId));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // DELETE api/auth/user/{id}  (soft delete -> IsActive = false)
    [HttpDelete("user/{id:int}")]
    public async Task<IActionResult> SoftDeleteUser(int id, CancellationToken ct)
    {
        var ok = await _auth.SoftDeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    // PUT api/auth/user/{id}/password
    [HttpPut("user/{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest("NewPassword is required.");
        var ok = await _auth.ChangePasswordAsync(id, req.CurrentPassword, req.NewPassword, ct);
        return ok ? NoContent() : Unauthorized();
    }
}

public record RegisterRequest(string Username, string Password, int RoleId);
public record LoginRequest(string Username, string Password);
public record UpdateAuthUserRequest(string Username, int RoleId, bool IsActive);
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
public record AuthUserResponse(int Id, string? Username, bool? IsActive, int? RoleId);
