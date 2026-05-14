using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface ICalendarService
{
    CalendarMonthResponse GetCalendarMonth(UserContext context, DateOnly month);
    IReadOnlyList<UpcomingEventResponse> GetUpcomingEvents(UserContext context, int limit);
    IReadOnlyList<UpcomingEventResponse> GetPastEvents(UserContext context, int limit);
}
