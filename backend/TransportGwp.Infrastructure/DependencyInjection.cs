using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransportGwp.Application;
using TransportGwp.Application.Interfaces;
using TransportGwp.Infrastructure.Ai;
using TransportGwp.Infrastructure.Geo;
using TransportGwp.Infrastructure.Persistence;

namespace TransportGwp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplication();

            services.AddDbContext<TransportDbContext>(o =>
                o.UseSqlite(configuration.GetConnectionString("Transports") ?? "Data Source=transports.db"));
            services.AddScoped<ITransportRepository, TransportRepository>();

            services.Configure<GoogleMapsOptions>(configuration.GetSection(GoogleMapsOptions.Section));
            services.AddMemoryCache();
            services.AddHttpClient(GeoService.GoogleClient, c => c.Timeout = TimeSpan.FromSeconds(15));
            services.AddHttpClient(GeoService.NominatimClient, c => c.Timeout = TimeSpan.FromSeconds(10));
            services.AddScoped<IGeoService, GeoService>();

            services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.Section));
            services.AddSingleton<ILlmClient, ClaudeLlmClient>();

            return services;
        }
    }
}
