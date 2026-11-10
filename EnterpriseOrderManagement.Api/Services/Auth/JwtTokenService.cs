using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EnterpriseOrderManagement.Api.Configuration;
using EnterpriseOrderManagement.Api.Models;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseOrderManagement.Api.Services.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public JwtTokenResult CreateAccessToken(
        ApplicationUser user,
        IList<string> roles
    )
    {
        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id
            ),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty
            ),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()
            ),

            new Claim(
                "role",
                roles.FirstOrDefault() ?? "User"
            )
        };

        var secretKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
        );

        var signingCredentials = new SigningCredentials(
            secretKey,
            SecurityAlgorithms.HmacSha256
        );

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(
            _jwtSettings.AccessTokenExpirationMinutes
        );

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials
        );

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new JwtTokenResult(
            accessToken,
            expiresAtUtc
        );
    }
}