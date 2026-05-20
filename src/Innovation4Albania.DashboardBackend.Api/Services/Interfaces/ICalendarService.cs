using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface ICalendarService
{
    Task<CalendarMonthResponse> GetCalendarMonth(UserContext context, DateOnly month);
    Task<IReadOnlyList<UpcomingEventResponse>> GetUpcomingEvents(UserContext context, int limit);
    Task<IReadOnlyList<UpcomingEventResponse>> GetPastEvents(UserContext context, int limit);
}
