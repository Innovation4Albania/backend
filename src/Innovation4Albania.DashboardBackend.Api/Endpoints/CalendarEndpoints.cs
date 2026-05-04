using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class CalendarEndpoints
{
    public static RouteGroupBuilder MapCalendarEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/calendar/month", (string role, string? ministry, DateOnly? month, IUserContextService contextService, ICalendarService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetCalendarMonth(context, month ?? DateOnly.FromDateTime(DateTime.Today)))
                : errorResult!;
        });

        api.MapGet("/calendar/upcoming", (string role, string? ministry, int? limit, IUserContextService contextService, ICalendarService service) =>
        {
            return EndpointContextResolver.TryResolve(role, ministry, contextService, out var context, out var errorResult)
                ? Results.Ok(service.GetUpcomingEvents(context, Math.Clamp(limit.GetValueOrDefault(8), 1, 50)))
                : errorResult!;
        });

        return api;
    }
}
