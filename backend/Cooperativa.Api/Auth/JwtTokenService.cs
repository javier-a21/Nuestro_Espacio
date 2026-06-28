using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cooperativa.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cooperativa.Api.Auth;

public class JwtTokenService
{
    private readonly JwtOptions _opt;
    public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

    public string Create(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? user.UserName ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_opt.ExpiryDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
