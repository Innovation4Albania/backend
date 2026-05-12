using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class AuthService(IInnovationDashboardRepository repository, IConfiguration configuration) : IAuthService
{
    public string? ValidateLogin(LoginRequest request) => repository.ValidateLogin(request);

    public AuthResponse Login(LoginRequest request)
    {
        var user = repository.Login(request);
        return new AuthResponse(CreateToken(user), user);
    }

    private string CreateToken(UserResponse user)
    {
        var signingKey = GetSigningKey(configuration);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(configuration.GetValue("Jwt:TokenLifetimeMinutes", 120));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new("role", user.Role),
            new("roleLabel", user.RoleLabel)
        };

        if (!string.IsNullOrWhiteSpace(user.Ministry))
        {
            claims.Add(new Claim("ministry", user.Ministry));
        }

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "Innovation4Albania",
            audience: configuration["Jwt:Audience"] ?? "Innovation4Albania.Frontend",
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static SymmetricSecurityKey GetSigningKey(IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 bytes.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}
