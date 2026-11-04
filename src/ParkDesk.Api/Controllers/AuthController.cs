using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ParkDesk.Api.Dtos;
using ParkDesk.Api.Services;

namespace ParkDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterCustomerAsync(request.Email, request.Password);
        return CreatedAtAction(nameof(Register), new AuthResponse(result.Token, result.ExpiresAt, result.Email, result.Role));
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        return Ok(new AuthResponse(result.Token, result.ExpiresAt, result.Email, result.Role));
    }
}
