using Innovation4Albania.DashboardBackend.Api.Configuration;
using Innovation4Albania.DashboardBackend.Api.Endpoints;
using Innovation4Albania.DashboardBackend.Api.Middleware;
using Innovation4Albania.DashboardBackend.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

LoadDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Innovation4Albania",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Innovation4Albania.Frontend",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = AuthService.GetSigningKey(builder.Configuration),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddApiJsonConfiguration()
    .AddConfiguredCors()
    .AddApplicationServices();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapHealthEndpoints();
api.MapReferenceDataEndpoints();
api.MapAuthEndpoints();

var protectedApi = api.MapGroup("").RequireAuthorization();
protectedApi.MapDashboardEndpoints();
protectedApi.MapProjectEndpoints();
protectedApi.MapPortfolioEndpoints();
protectedApi.MapUpdateEndpoints();
protectedApi.MapCalendarEndpoints();
protectedApi.MapAiEndpoints();

app.Run();

static void LoadDotEnv(string path)
{
    if (!File.Exists(path))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim().Trim('"');
        if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
        {
            continue;
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}
