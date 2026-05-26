using Innovation4Albania.DashboardBackend.Api.Endpoints;
using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/auth/login", async (LoginRequest request, HttpContext httpContext, IAuthService service, ILogger<OperationAuditLog> auditLogger) =>
        {
            var result = await service.TryLoginAsync(request);
            if (!result.IsSuccess)
            {
                auditLogger.LogWarning(
                    "Login rejected for role {Role} from IP {RemoteIp}.",
                    request.Role,
                    httpContext.Connection.RemoteIpAddress);
                return Results.BadRequest(new ApiErrorResponse("validation_error", result.Error!));
            }

            var response = result.Response!;
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

        api.MapPost("/auth/refresh", async (ClaimsPrincipal user, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var newToken = await authService.RefreshTokenAsync(context);
            if (newToken is null)
            {
                return Results.Unauthorized();
            }

            auditLogger.LogInformation("Authentication token refreshed for role {Role}.", context.Role);
            return Results.Ok(new { token = newToken });
        }).RequireAuthorization();

        api.MapGet("/auth/users", async (ClaimsPrincipal user, IUserContextService contextService, IAuthService authService) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManageUsers(context.Role))
            {
                return Results.Forbid();
            }

            return Results.Ok(await authService.GetManagedUsersAsync(context));
        }).RequireAuthorization();

        api.MapPost("/auth/users", async (ClaimsPrincipal user, CreateUserRequest request, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManageUsers(context.Role))
            {
                return Results.Forbid();
            }

            var result = await authService.CreateUserAsync(context, request);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new ApiErrorResponse("user_create_failed", result.Error!));
            }

            auditLogger.LogInformation("User account {UserId} created by role {Role}.", result.Response!.Id, context.Role);
            return Results.Ok(result.Response);
        }).RequireAuthorization();

        api.MapPut("/auth/users/{id}", async (string id, ClaimsPrincipal user, UpdateManagedUserRequest request, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManageUsers(context.Role))
            {
                return Results.Forbid();
            }

            var result = await authService.UpdateUserAsync(context, id, request);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new ApiErrorResponse("user_update_failed", result.Error!));
            }

            auditLogger.LogInformation("User account {UserId} updated by role {Role}.", id, context.Role);
            return Results.Ok(result.Response);
        }).RequireAuthorization();

        api.MapPut("/auth/users/{id}/password", async (string id, ClaimsPrincipal user, AdminResetPasswordRequest request, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManageUsers(context.Role))
            {
                return Results.Forbid();
            }

            var result = await authService.ResetPasswordAsync(context, id, request);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new ApiErrorResponse("password_reset_failed", result.Error!));
            }

            auditLogger.LogInformation("Password reset for user {UserId} by role {Role}.", id, context.Role);
            return Results.NoContent();
        }).RequireAuthorization();

        api.MapDelete("/auth/users/{id}", async (string id, ClaimsPrincipal user, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManageUsers(context.Role))
            {
                return Results.Forbid();
            }

            var result = await authService.DeactivateUserAsync(context, id);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new ApiErrorResponse("user_deactivate_failed", result.Error!));
            }

            auditLogger.LogInformation("User account {UserId} deactivated by role {Role}.", id, context.Role);
            return Results.NoContent();
        }).RequireAuthorization();

        api.MapPut("/auth/users/{id}/activate", async (string id, ClaimsPrincipal user, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            if (!ApplicationRoles.CanManageUsers(context.Role))
            {
                return Results.Forbid();
            }

            var result = await authService.ActivateUserAsync(context, id);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new ApiErrorResponse("user_activate_failed", result.Error!));
            }

            auditLogger.LogInformation("User account {UserId} activated by role {Role}.", id, context.Role);
            return Results.NoContent();
        }).RequireAuthorization();

        api.MapPut("/auth/me/credentials", async (ClaimsPrincipal user, ChangeOwnCredentialsRequest request, IUserContextService contextService, IAuthService authService, ILogger<OperationAuditLog> auditLogger) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var result = await authService.ChangeOwnCredentialsAsync(context, request);
            if (!result.IsSuccess)
            {
                return Results.BadRequest(new ApiErrorResponse("credentials_update_failed", result.Error!));
            }

            auditLogger.LogInformation("Credentials updated for account {UserId}.", result.Response!.User.Id);
            return Results.Ok(result.Response);
        }).RequireAuthorization();

        return api;
    }
}
