using Colosoft.Mediator;
using Colosoft.Mediator.Pipeline;
using Colosoft.Mediator.Registration;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediator(
            this IServiceCollection services,
            Action<MediatorServiceConfiguration> configuration)
        {
            var serviceConfig = new MediatorServiceConfiguration();

            configuration.Invoke(serviceConfig);

            return services.AddMediator(serviceConfig);
        }

        public static IServiceCollection AddMediator(
            this IServiceCollection services,
            MediatorServiceConfiguration configuration)
        {
            if (!configuration.AssembliesToRegister.Any())
            {
                throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");
            }

            ServiceRegistrar.AddMediatorClasses(services, configuration);

            ServiceRegistrar.AddRequiredServices(services, configuration);

            return services;
        }
    }
}