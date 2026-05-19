using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class AuthService(IInnovationDashboardRepository repository, IConfiguration configuration) : IAuthService
{
    public string? ValidateLogin(LoginRequest request)
    {
        var context = UserContext.From(request.Role, request.Ministry);
        if (!ApplicationRoles.CanUseInteractiveLogin(context.Role))
        {
            return "Ky rol ka akses vetëm me link view.";
        }

        var validationError = repository.ValidateLogin(request);
        if (validationError is not null)
        {
            return validationError;
        }

        return ValidateCredentials(context.Role, request.Username, request.Password);
    }

    public string? ValidateViewLink(LoginRequest request)
    {
        var context = UserContext.From(request.Role, request.Ministry);
        if (!ApplicationRoles.IsViewOnlyRole(context.Role))
        {
            return "Ky rol duhet të përdorë login.";
        }

        return repository.ValidateLogin(request);
    }

    public AuthResponse Login(LoginRequest request)
    {
        var user = repository.Login(request);
        return new AuthResponse(CreateToken(user), user);
    }

    public AuthResponse CreateViewLinkSession(LoginRequest request)
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

    private string? ValidateCredentials(string role, string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return "Username dhe password janë të detyrueshme.";
        }

        var configuredUsername = configuration[$"Auth:Users:{role}:Username"];
        var configuredPassword = configuration[$"Auth:Users:{role}:Password"];
        if (string.IsNullOrWhiteSpace(configuredUsername) || string.IsNullOrWhiteSpace(configuredPassword))
        {
            return "Kredencialet për këtë rol nuk janë konfiguruar.";
        }

        return string.Equals(username.Trim(), configuredUsername, StringComparison.Ordinal) &&
        BCrypt.Net.BCrypt.Verify(password, configuredPassword)
     ? null
     : "Username ose password nuk është i saktë.";
    }
}
