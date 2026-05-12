namespace Innovation4Albania.DashboardBackend.Api.Configuration;

public static class CorsConfiguration
{
    public static IServiceCollection AddConfiguredCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? string.Empty)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var defaultOrigins = new[]
                {
                    "http://localhost:5173",
                    "https://localhost:5173",
                    "http://127.0.0.1:5173",
                    "http://localhost:8080",
                    "http://127.0.0.1:8080",
                    "http://localhost:3000",
                    "https://localhost:3000"
                };

                var origins = allowedOrigins.Length > 0 ? allowedOrigins : defaultOrigins;

                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
