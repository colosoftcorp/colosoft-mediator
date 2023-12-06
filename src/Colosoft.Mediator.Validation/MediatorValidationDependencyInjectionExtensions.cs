using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

namespace Colosoft.Mediator
{
    public static class MediatorValidationDependencyInjectionExtensions
    {
        public static IServiceCollection AddValidators(this IServiceCollection services, System.Reflection.Assembly assembly)
        {
            var validatorType = typeof(FluentValidation.IValidator);
            var genericValidatorType = typeof(FluentValidation.IValidator<>);
            var baseRequestType = typeof(IBaseRequest);

            foreach (var type in assembly.GetTypes()
                .Where(type => validatorType.IsAssignableFrom(type)))
            {
                foreach (var @interface in type.GetInterfaces())
                {
                    if (!@interface.IsGenericType)
                    {
                        continue;
                    }

                    var typeDefinition = @interface.GetGenericTypeDefinition();

                    if (typeDefinition != genericValidatorType)
                    {
                        continue;
                    }

                    var requestType = @interface.GetGenericArguments()[0];

                    if (baseRequestType.IsAssignableFrom(requestType))
                    {
                        var registerType = genericValidatorType.MakeGenericType(requestType);
                        services.TryAddTransient(registerType, type);
                    }
                }
            }

            return services;
        }
    }
}
