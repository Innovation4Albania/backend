using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class AuthService(
    IInnovationDashboardRepository dashboardRepository,
    IUserRepository userRepository,
    IConfiguration configuration) : Interfaces.IAuthService
{
    public async Task<(bool IsSuccess, AuthResponse? Response, string? Error)> TryLoginAsync(LoginRequest request)
    {
        var role = request.Role.Trim();
        if (!ApplicationRoles.CanUseInteractiveLogin(role))
        {
            return (false, null, "Ky rol ka akses vetëm me link view.");
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, null, "Username dhe fjalëkalimi janë të detyrueshme.");
        }

        var account = await userRepository.GetUserByUsername(request.Username);
        if (account is null ||
            !account.IsActive ||
            !CanUseLoginOptionForAccount(role, account.Role) ||
            !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            return (false, null, "Username ose fjalëkalimi nuk është i saktë.");
        }

        var accountMinistry = ApplicationRoles.FixedMinistryForRole(account.Role) ?? account.Ministry;
        var context = UserContext.From(account.Role, accountMinistry, account.Username, account.FullName, account.Id);
        if (account.Role != ApplicationRoles.Admin && !dashboardRepository.IsValidContext(context, out var contextError))
        {
            return (false, null, contextError);
        }

        var user = ToUserResponse(account);
        return (true, new AuthResponse(CreateToken(user, account.Username), user), null);
    }

    private static bool CanUseLoginOptionForAccount(string requestedRole, string accountRole) =>
        string.Equals(accountRole, requestedRole, StringComparison.Ordinal) ||
        (requestedRole == ApplicationRoles.DrejtorAgjencie &&
            (accountRole is ApplicationRoles.Admin or ApplicationRoles.DrejtorInovacioniPublik or ApplicationRoles.StafMinistrie ||
                ApplicationRoles.IsAgencyContributor(accountRole)));

    public string? ValidateViewLink(LoginRequest request)
    {
        var context = UserContext.From(request.Role, request.Ministry);
        if (!ApplicationRoles.IsViewOnlyRole(context.Role))
        {
            return "Ky rol duhet të përdorë login.";
        }

        return dashboardRepository.ValidateLogin(request);
    }

    public AuthResponse CreateViewLinkSession(LoginRequest request)
    {
        var user = dashboardRepository.Login(request);
        return new AuthResponse(CreateToken(user, null), user);
    }

    public async Task<string?> RefreshTokenAsync(UserContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Username))
        {
            var account = await userRepository.GetUserByUsername(context.Username);
            if (account is null || !account.IsActive || !string.Equals(account.Role, context.Role, StringComparison.Ordinal))
            {
                return null;
            }

            return CreateToken(ToUserResponse(account), account.Username);
        }

        var viewUser = dashboardRepository.Login(new LoginRequest(context.Role, context.Ministry, Name: null));
        return CreateToken(viewUser, null);
    }

    public async Task<IReadOnlyList<ManagedUserResponse>> GetManagedUsersAsync(UserContext context)
    {
        if (!ApplicationRoles.CanReadManagedUsers(context.Role))
        {
            return [];
        }

        return (await userRepository.GetUsers())
            .Where(user => IsManagedUserAccount(user.Role))
            .ToList();
    }

    public async Task<(bool IsSuccess, ManagedUserResponse? Response, string? Error)> CreateUserAsync(UserContext context, CreateUserRequest request)
    {
        if (!ApplicationRoles.CanManageUsers(context.Role))
        {
            return (false, null, "Ky rol nuk mund të krijojë llogari.");
        }

        if (!IsManagedUserAccount(request.Role))
        {
            return (false, null, "Ky rol nuk mund të krijohet nga administrimi.");
        }

        var validationError = ValidateNewCredentials(request.FullName, request.Username, request.Password);
        if (validationError is not null)
        {
            return (false, null, validationError);
        }

        var ministry = ApplicationRoles.FixedMinistryForRole(request.Role)
            ?? (ApplicationRoles.RequiresMinistry(request.Role) ? request.Ministry : null);
        if (ApplicationRoles.RequiresMinistry(request.Role) &&
            !dashboardRepository.IsValidContext(UserContext.From(request.Role, ministry), out var ministryError))
        {
            return (false, null, ministryError);
        }

        return await userRepository.CreateUser(request with { Ministry = ministry }, BCrypt.Net.BCrypt.HashPassword(request.Password));
    }

    public async Task<(bool IsSuccess, ManagedUserResponse? Response, string? Error)> UpdateUserAsync(
        UserContext context,
        string id,
        UpdateManagedUserRequest request)
    {
        if (!ApplicationRoles.CanManageUsers(context.Role))
        {
            return (false, null, "Ky rol nuk mund të modifikojë llogari.");
        }

        var account = await userRepository.GetUserById(id);
        if (account is null || !IsManagedUserAccount(account.Role))
        {
            return (false, null, "Llogaria e menaxhueshme nuk u gjet.");
        }

        if (!IsManagedUserAccount(request.Role))
        {
            return (false, null, "Ky rol nuk mund të zgjidhet nga administrimi.");
        }

        var ministry = ApplicationRoles.FixedMinistryForRole(request.Role)
            ?? (ApplicationRoles.RequiresMinistry(request.Role) ? request.Ministry : null);
        if (ApplicationRoles.RequiresMinistry(request.Role) &&
            !dashboardRepository.IsValidContext(UserContext.From(request.Role, ministry), out var ministryError))
        {
            return (false, null, ministryError);
        }

        var validationError = ValidateIdentity(request.FullName, request.Username);
        if (validationError is not null)
        {
            return (false, null, validationError);
        }

        string? passwordHash = null;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var passwordError = ValidatePassword(request.Password);
            if (passwordError is not null)
            {
                return (false, null, passwordError);
            }

            passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        var result = await userRepository.UpdateUser(id, request.FullName, request.Username, request.Role, ministry, passwordHash);
        if (!result.IsSuccess)
        {
            return (false, null, result.Error);
        }

        var updated = await userRepository.GetUserById(id);
        return updated is null
            ? (false, null, "Llogaria e menaxhueshme nuk u gjet.")
            : (true, ToManagedUserResponse(updated), null);
    }

    public async Task<(bool IsSuccess, string? Error)> ResetPasswordAsync(UserContext context, string id, AdminResetPasswordRequest request)
    {
        if (!ApplicationRoles.CanManageUsers(context.Role))
        {
            return (false, "Ky rol nuk mund të ndryshojë fjalëkalime.");
        }

        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
        {
            return (false, passwordError);
        }

        var account = await userRepository.GetUserById(id);
        if (account is null || !IsManagedUserAccount(account.Role))
        {
            return (false, "Llogaria e menaxhueshme nuk u gjet.");
        }

        return await userRepository.UpdatePassword(id, BCrypt.Net.BCrypt.HashPassword(request.Password));
    }

    public async Task<(bool IsSuccess, string? Error)> DeactivateUserAsync(UserContext context, string id)
    {
        if (!ApplicationRoles.CanManageUsers(context.Role))
        {
            return (false, "Ky rol nuk mund të çaktivizojë llogari.");
        }

        var account = await userRepository.GetUserById(id);
        if (account is null || !IsManagedUserAccount(account.Role))
        {
            return (false, "Llogaria e menaxhueshme nuk u gjet.");
        }

        return await userRepository.DeactivateUser(id);
    }

    public async Task<(bool IsSuccess, string? Error)> ActivateUserAsync(UserContext context, string id)
    {
        if (!ApplicationRoles.CanManageUsers(context.Role))
        {
            return (false, "Ky rol nuk mund të aktivizojë llogari.");
        }

        var account = await userRepository.GetUserById(id);
        if (account is null || !IsManagedUserAccount(account.Role))
        {
            return (false, "Llogaria e menaxhueshme nuk u gjet.");
        }

        return await userRepository.ActivateUser(id);
    }

    public async Task<(bool IsSuccess, string? Error)> DeleteUserAsync(UserContext context, string id)
    {
        if (!ApplicationRoles.CanManageUsers(context.Role))
        {
            return (false, "Ky rol nuk mund të fshijë llogari.");
        }

        if (string.Equals(context.UserId, id, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Nuk mund të fshish llogarinë me të cilën je i/e loguar.");
        }

        var account = await userRepository.GetUserById(id);
        if (account is null || !IsManagedUserAccount(account.Role))
        {
            return (false, "Llogaria e menaxhueshme nuk u gjet.");
        }

        return await userRepository.DeleteUser(id);
    }

    private static bool IsManagedUserAccount(string role) =>
        role is ApplicationRoles.Kryeminister
            or ApplicationRoles.Minister
            or ApplicationRoles.MinisterEkonomiseInovacionit
            or ApplicationRoles.Admin
            or ApplicationRoles.DrejtorAgjencie
            or ApplicationRoles.DrejtorInovacioniPublik
            or ApplicationRoles.StafAgjencie
            or ApplicationRoles.Ekspert
            or ApplicationRoles.Specialist
            or ApplicationRoles.StafMinistrie;

    public async Task<(bool IsSuccess, AuthResponse? Response, string? Error)> ChangeOwnCredentialsAsync(
        UserContext context,
        ChangeOwnCredentialsRequest request)
    {
        if (string.IsNullOrWhiteSpace(context.Username))
        {
            return (false, null, "Pamjet pa kredenciale nuk kanë llogari për të ndryshuar.");
        }

        var account = await userRepository.GetUserByUsername(context.Username);
        if (account is null || !account.IsActive || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.PasswordHash))
        {
            return (false, null, "Fjalëkalimi aktual nuk është i saktë.");
        }

        var username = string.IsNullOrWhiteSpace(request.Username) ? account.Username : request.Username.Trim();
        if (username.Length < 3)
        {
            return (false, null, "Username duhet të ketë të paktën 3 karaktere.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) && string.Equals(username, account.Username, StringComparison.OrdinalIgnoreCase))
        {
            return (false, null, "Vendos username të ri ose fjalëkalim të ri.");
        }

        var passwordHash = default(string);
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var passwordError = ValidatePassword(request.NewPassword);
            if (passwordError is not null)
            {
                return (false, null, passwordError);
            }

            passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        var updated = await userRepository.UpdateCredentials(account.Id, username, passwordHash);
        if (!updated.IsSuccess)
        {
            return (false, null, updated.Error);
        }

        var nextUser = new UserResponse(account.Id, account.FullName, account.Role, account.Ministry, ApplicationRoles.ToDisplayLabel(account.Role));
        return (true, new AuthResponse(CreateToken(nextUser, username), nextUser), null);
    }

    private string CreateToken(UserResponse user, string? username)
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

        if (!string.IsNullOrWhiteSpace(username))
        {
            claims.Add(new Claim("username", username.Trim()));
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

    private static UserResponse ToUserResponse(StoredUser account) =>
        new(account.Id, account.FullName, account.Role, ApplicationRoles.FixedMinistryForRole(account.Role) ?? account.Ministry, ApplicationRoles.ToDisplayLabel(account.Role));

    private static ManagedUserResponse ToManagedUserResponse(StoredUser account) =>
        new(account.Id, account.Username, account.Role, ApplicationRoles.FixedMinistryForRole(account.Role) ?? account.Ministry, account.FullName, account.CreatedAt, account.IsActive);

    private static string? ValidateNewCredentials(string fullName, string username, string password)
    {
        var identityError = ValidateIdentity(fullName, username);
        if (identityError is not null)
        {
            return identityError;
        }

        return ValidatePassword(password);
    }

    private static string? ValidateIdentity(string fullName, string username)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Emri i plotë është i detyrueshëm.";
        }

        return string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3
            ? "Username duhet të ketë të paktën 3 karaktere."
            : null;
    }

    private static string? ValidatePassword(string? password) =>
        string.IsNullOrWhiteSpace(password) || password.Length < 8
            ? "Fjalëkalimi duhet të ketë të paktën 8 karaktere."
            : null;
}
