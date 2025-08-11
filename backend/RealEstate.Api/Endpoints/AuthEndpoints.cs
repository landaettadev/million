using Microsoft.AspNetCore.Mvc;
using RealEstate.Application;

namespace RealEstate.Api.Endpoints;

[ApiController]
[Route("api/admin/auth")]
public class AuthEndpoints : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthEndpoints(IAuthService authService)
    {
        _authService = authService;
    }

    public sealed record LoginRequest(string email, string password);

    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.email) || string.IsNullOrWhiteSpace(request.password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        var result = await _authService.LoginAsync(request.email, request.password, ct);
        if (result is null)
        {
            return Unauthorized();
        }

        var (token, user) = result.Value;
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
}


