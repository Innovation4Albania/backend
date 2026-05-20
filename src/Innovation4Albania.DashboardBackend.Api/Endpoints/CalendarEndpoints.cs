using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class CalendarEndpoints
{
    public static RouteGroupBuilder MapCalendarEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/calendar/month", async (ClaimsPrincipal user, DateOnly? month, IUserContextService contextService, ICalendarService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetCalendarMonth(context, month ?? DateOnly.FromDateTime(DateTime.Today)));
        });

        api.MapGet("/calendar/upcoming", async (ClaimsPrincipal user, int? limit, IUserContextService contextService, ICalendarService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetUpcomingEvents(context, Math.Clamp(limit.GetValueOrDefault(8), 1, 50)));
        });

        api.MapGet("/calendar/past", async (ClaimsPrincipal user, int? limit, IUserContextService contextService, ICalendarService service) =>
        {
            if (!EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult))
                return errorResult!;

            return Results.Ok(await service.GetPastEvents(context, Math.Clamp(limit.GetValueOrDefault(12), 1, 50)));
        });

        return api;
    }
}
