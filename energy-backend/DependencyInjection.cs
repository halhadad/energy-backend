using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using energy_backend.Application;
using energy_backend.Infrastructure;
using Microsoft.Extensions.DependencyInjection;


namespace energy_backend.Api
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddApplicationServices()
                .AddInfrastructureServices();
            return services;
        }
    }
}
