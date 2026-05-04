using System.Text.Json.Serialization;

namespace Innovation4Albania.DashboardBackend.Api.Configuration;

public static class JsonConfiguration
{
    public static IServiceCollection AddApiJsonConfiguration(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.WriteIndented = true;
        });

        return services;
    }
}
