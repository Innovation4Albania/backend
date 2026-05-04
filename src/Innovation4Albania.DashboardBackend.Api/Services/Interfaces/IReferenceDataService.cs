using Innovation4Albania.DashboardBackend.Api.Models;

namespace Innovation4Albania.DashboardBackend.Api.Services.Interfaces;

public interface IReferenceDataService
{
    IReadOnlyList<string> GetMinistries();
    IReadOnlyList<ReferenceOptionResponse> GetRoles();
    IReadOnlyList<StatusReferenceResponse> GetStatuses();
    IReadOnlyList<ReferenceOptionResponse> GetPriorities();
    IReadOnlyList<ReferenceOptionResponse> GetSectors();
    IReadOnlyList<ReferenceOptionResponse> GetWorkgroupRoles();
}
