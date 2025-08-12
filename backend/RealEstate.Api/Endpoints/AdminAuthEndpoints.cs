using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthEndpoints : ControllerBase
{
    private readonly IAuthService _authService;

    public AdminAuthEndpoints(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        var result = await _authService.LoginAsync(request.Email, request.Password, ct);
        
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = result.Value.token;
        var user = result.Value.user;
        
        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id,
                email = user.Email,
                name = user.Name,
                role = user.Role
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct = default)
    {
        // TODO: Implement token refresh logic
        return Ok(new { message = "Token refresh endpoint - not yet implemented" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        // TODO: Implement logout logic (e.g., blacklist token)
        return Ok(new { message = "Logout successful" });
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string Token);
