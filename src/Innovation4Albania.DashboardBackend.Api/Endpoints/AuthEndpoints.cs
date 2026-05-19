using Innovation4Albania.DashboardBackend.Api.Endpoints;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/auth/login", (LoginRequest request, IAuthService service) =>
        {
            var validationError = service.ValidateLogin(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new ApiErrorResponse("validation_error", validationError));
            }

            return Results.Ok(service.Login(request));
        });

        api.MapPost("/auth/view-link", (LoginRequest request, IAuthService service) =>
        {
            var validationError = service.ValidateViewLink(request);
            if (validationError is not null)
            {
                return Results.BadRequest(new ApiErrorResponse("validation_error", validationError));
            }

            return Results.Ok(service.CreateViewLinkSession(request));
        });

        api.MapPost("/auth/refresh", (ClaimsPrincipal user, IUserContextService contextService, IAuthService authService) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
            {
                return errorResult!;
            }

            var newToken = authService.RefreshToken(context);
            return Results.Ok(new { token = newToken });
        }).RequireAuthorization();


        return api;
    }
}
