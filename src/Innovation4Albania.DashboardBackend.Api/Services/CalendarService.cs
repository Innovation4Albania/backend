using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class CalendarService(IInnovationDashboardRepository repository) : ICalendarService
{
    public Task<CalendarMonthResponse> GetCalendarMonth(UserContext context, DateOnly month) => repository.GetCalendarMonth(context, month);
    public Task<IReadOnlyList<UpcomingEventResponse>> GetUpcomingEvents(UserContext context, int limit) => repository.GetUpcomingEvents(context, limit);
    public Task<IReadOnlyList<UpcomingEventResponse>> GetPastEvents(UserContext context, int limit) => repository.GetPastEvents(context, limit);
}
