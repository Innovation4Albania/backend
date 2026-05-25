using Innovation4Albania.DashboardBackend.Api.Endpoints;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/auth/login", (LoginRequest request, HttpContext httpContext, IAuthService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            var validationError = service.ValidateLogin(request);
            if (validationError is not null)
            {
                auditLogger.LogWarning(
                    "Login rejected for role {Role} from IP {RemoteIp}.",
                    request.Role,
                    httpContext.Connection.RemoteIpAddress);
                return Results.BadRequest(new ApiErrorResponse("validation_error", validationError));
            }

            var response = service.Login(request);
            auditLogger.LogInformation("Login succeeded for user {UserId} with role {Role}.", response.User.Id, response.User.Role);
            return Results.Ok(response);
        }).RequireRateLimiting("auth-login");

        api.MapPost("/auth/view-link", (LoginRequest request, IAuthService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            var validationError = service.ValidateViewLink(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new ApiErrorResponse("validation_error", validationError));
            }

            var response = service.CreateViewLinkSession(request);
            auditLogger.LogInformation("View session created for role {Role} and ministry {Ministry}.", response.User.Role, response.User.Ministry);
            return Results.Ok(response);
        });

        api.MapPost("/auth/refresh", (ClaimsPrincipal user, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var newToken = authService.RefreshToken(context);
            auditLogger.LogInformation("Authentication token refreshed for role {Role}.", context.Role);
            return Results.Ok(new { token = newToken });
        }).RequireAuthorization();


        return api;
    }
}
