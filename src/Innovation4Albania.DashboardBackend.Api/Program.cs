using Innovation4Albania.DashboardBackend.Api.Configuration;
using Innovation4Albania.DashboardBackend.Api.Endpoints;
using Innovation4Albania.DashboardBackend.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiJsonConfiguration()
    .AddConfiguredCors()
    .AddApplicationServices();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();

var api = app.MapGroup("/api");
api.MapHealthEndpoints();
api.MapReferenceDataEndpoints();
api.MapAuthEndpoints();
api.MapDashboardEndpoints();
api.MapProjectEndpoints();
api.MapPortfolioEndpoints();
api.MapUpdateEndpoints();
api.MapCalendarEndpoints();
api.MapAiEndpoints();

app.Run();
