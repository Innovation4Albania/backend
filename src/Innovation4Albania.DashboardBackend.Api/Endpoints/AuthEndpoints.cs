using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

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

        return api;
    }
}
