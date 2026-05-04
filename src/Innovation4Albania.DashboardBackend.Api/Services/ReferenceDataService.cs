using Innovation4Albania.DashboardBackend.Api.Constants;
using Innovation4Albania.DashboardBackend.Api.Data.Repositories;
using Innovation4Albania.DashboardBackend.Api.Models;
using Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

namespace Innovation4Albania.DashboardBackend.Api.Services;

public sealed class ReferenceDataService(IInnovationDashboardRepository repository) : IReferenceDataService
{
    public IReadOnlyList<string> GetMinistries() => repository.GetMinistries();
    public IReadOnlyList<ReferenceOptionResponse> GetRoles() => ApplicationRoles.All.Select(role => new ReferenceOptionResponse(role, ApplicationRoles.ToDisplayLabel(role))).ToList();
    public IReadOnlyList<StatusReferenceResponse> GetStatuses() => ProjectStatuses.All.Select(status => new StatusReferenceResponse(status, ProjectStatuses.ToLabel(status), ProjectStatuses.ToColor(status))).ToList();
    public IReadOnlyList<ReferenceOptionResponse> GetPriorities() => ProjectPriorities.All.Select(priority => new ReferenceOptionResponse(priority, ProjectPriorities.ToLabel(priority))).ToList();
    public IReadOnlyList<ReferenceOptionResponse> GetSectors() => ProjectSectors.All.Select(sector => new ReferenceOptionResponse(sector, ProjectSectors.ToLabel(sector))).ToList();
    public IReadOnlyList<ReferenceOptionResponse> GetWorkgroupRoles() => WorkgroupRoles.All.Select(role => new ReferenceOptionResponse(role, WorkgroupRoles.ToLabel(role))).ToList();
}
