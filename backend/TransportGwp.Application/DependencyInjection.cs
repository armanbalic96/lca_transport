using Microsoft.Extensions.DependencyInjection;
using TransportGwp.Application.Implementation;
using TransportGwp.Application.Interfaces;

namespace TransportGwp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ITransportCalculator, TransportCalculator>();
            services.AddSingleton<IExportService, ExportService>();
            services.AddScoped<ITransportRegisterService, TransportRegisterService>();
            services.AddScoped<IAiAdvisor, AiAdvisor>();
            return services;
        }
    }
}
