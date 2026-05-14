using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;
using System.Security.Claims;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class CalendarEndpoints
{
    public static RouteGroupBuilder MapCalendarEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/calendar/month", (ClaimsPrincipal user, DateOnly? month, IUserContextService contextService, ICalendarService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetCalendarMonth(context, month ?? DateOnly.FromDateTime(DateTime.Today)))
                : errorResult!;
        });

        api.MapGet("/calendar/upcoming", (ClaimsPrincipal user, int? limit, IUserContextService contextService, ICalendarService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetUpcomingEvents(context, Math.Clamp(limit.GetValueOrDefault(8), 1, 50)))
                : errorResult!;
        });

        api.MapGet("/calendar/past", (ClaimsPrincipal user, int? limit, IUserContextService contextService, ICalendarService service) =>
        {
            return EndpointContextResolver.TryResolve(user, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetPastEvents(context, Math.Clamp(limit.GetValueOrDefault(12), 1, 50)))
                : errorResult!;
        });

        return api;
    }
}
