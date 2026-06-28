using Cooperativa.Api.Auth;
using Cooperativa.Api.Contracts;
using Cooperativa.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cooperativa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly JwtTokenService _jwt;

    public AuthController(UserManager<AppUser> users, JwtTokenService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var user = new AppUser { UserName = req.Email, Email = req.Email, DisplayName = req.DisplayName };
        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok(new AuthResponse(_jwt.Create(user), user.Id, user.DisplayName, user.CooperativeId, user.Role?.ToString()));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, req.Password))
            return Unauthorized();

        return Ok(new AuthResponse(_jwt.Create(user), user.Id, user.DisplayName, user.CooperativeId, user.Role?.ToString()));
    }
}
