namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", () => Results.Ok(new
        {
            service = "Innovation4Albania Dashboard Backend",
            status = "ok",
            timestamp = DateTimeOffset.UtcNow
        }));

        return api;
    }
}
