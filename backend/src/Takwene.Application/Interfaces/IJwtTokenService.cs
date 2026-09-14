using Takwene.Application.DTOs.Auth;

namespace Takwene.Application.Interfaces;

public interface IJwtTokenService
{
    LoginResponse GenerateToken(string email, string role = "Admin");
}
