using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static RouteGroupBuilder MapReferenceDataEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/reference-data/ministries", (IReferenceDataService service) => Results.Ok(service.GetMinistries()));
        api.MapGet("/reference-data/roles", (IReferenceDataService service) => Results.Ok(service.GetRoles()));
        api.MapGet("/reference-data/statuses", (IReferenceDataService service) => Results.Ok(service.GetStatuses()));
        api.MapGet("/reference-data/priorities", (IReferenceDataService service) => Results.Ok(service.GetPriorities()));
        api.MapGet("/reference-data/sectors", (IReferenceDataService service) => Results.Ok(service.GetSectors()));
        api.MapGet("/reference-data/workgroup-roles", (IReferenceDataService service) => Results.Ok(service.GetWorkgroupRoles()));

        return api;
    }
}
