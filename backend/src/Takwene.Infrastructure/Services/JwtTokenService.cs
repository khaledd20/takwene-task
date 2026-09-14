using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Takwene.Application.DTOs.Auth;
using Takwene.Application.Interfaces;

namespace Takwene.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponse GenerateToken(string email, string role = "Admin")
    {
        var secret = _configuration["Jwt:Secret"] ?? "TakweneSuperSecretMasterKeyMustBeAtLeast32BytesLongForSecurity!";
        var issuer = _configuration["Jwt:Issuer"] ?? "TakweneApi";
        var audience = _configuration["Jwt:Audience"] ?? "TakweneClient";
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryHours"], out var hours) ? hours : 24;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResponse
        {
            Token = tokenString,
            Email = email,
            Role = role,
            ExpiresAt = expiresAt
        };
    }
}
