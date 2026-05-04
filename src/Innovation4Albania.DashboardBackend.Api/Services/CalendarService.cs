using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class CalendarService(IInnovationDashboardRepository repository) : ICalendarService
{
    public CalendarMonthResponse GetCalendarMonth(UserContext context, DateOnly month) => repository.GetCalendarMonth(context, month);
    public IReadOnlyList<object> GetUpcomingEvents(UserContext context, int limit) => repository.GetUpcomingEvents(context, limit);
}
