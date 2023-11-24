﻿using Colosoft.Mediator.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Colosoft.Mediator.Registration
{
    public static class ServiceRegistrar
    {
        public static void AddMediatorClasses(
            IServiceCollection services,
            MediatorServiceConfiguration configuration)
        {
            var assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

            ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>), services, assembliesToScan, false, configuration);
            ConnectImplementationsToTypesClosing(typeof(IRequestHandler<>), services, assembliesToScan, false, configuration);
            ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>), services, assembliesToScan, true, configuration);
            ConnectImplementationsToTypesClosing(typeof(IStreamRequestHandler<,>), services, assembliesToScan, false, configuration);
            ConnectImplementationsToTypesClosing(typeof(IRequestExceptionHandler<,,>), services, assembliesToScan, true, configuration);
            ConnectImplementationsToTypesClosing(typeof(IRequestExceptionAction<,>), services, assembliesToScan, true, configuration);

            if (configuration.AutoRegisterRequestProcessors)
            {
                ConnectImplementationsToTypesClosing(typeof(IRequestPreProcessor<>), services, assembliesToScan, false, configuration);
                ConnectImplementationsToTypesClosing(typeof(IRequestPostProcessor<,>), services, assembliesToScan, false, configuration);
            }

            var multiOpenInterfaces = new List<Type>
            {
                typeof(INotificationHandler<>),
                typeof(IRequestExceptionHandler<,,>),
                typeof(IRequestExceptionAction<,>),
            };

            if (configuration.AutoRegisterRequestProcessors)
            {
                multiOpenInterfaces.Add(typeof(IRequestPreProcessor<>));
                multiOpenInterfaces.Add(typeof(IRequestPostProcessor<,>));
            }

            foreach (var multiOpenInterface in multiOpenInterfaces)
            {
                var arity = multiOpenInterface.GetGenericArguments().Length;

                var concretions = assembliesToScan
                    .SelectMany(a => a.DefinedTypes)
                    .Where(type => type.FindInterfacesThatClose(multiOpenInterface).Any())
                    .Where(type => type.IsConcrete() && type.IsOpenGeneric())
                    .Where(type => type.GetGenericArguments().Length == arity)
                    .Where(configuration.TypeEvaluator)
                    .ToList();

                foreach (var type in concretions)
                {
                    services.AddTransient(multiOpenInterface, type);
                }
            }
        }

        private static void ConnectImplementationsToTypesClosing(
            Type openRequestInterface,
            IServiceCollection services,
            IEnumerable<Assembly> assembliesToScan,
            bool addIfAlreadyExists,
            MediatorServiceConfiguration configuration)
        {
            var concretions = new List<Type>();
            var interfaces = new List<Type>();
            foreach (var type in assembliesToScan.SelectMany(a => a.DefinedTypes).Where(t => !t.IsOpenGeneric()).Where(configuration.TypeEvaluator))
            {
                var interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();
                if (!interfaceTypes.Any())
                {
                    continue;
                }

                if (type.IsConcrete())
                {
                    concretions.Add(type);
                }

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaces.Fill(interfaceType);
                }
            }

            foreach (var @interface in interfaces)
            {
                var exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();
                if (addIfAlreadyExists)
                {
                    foreach (var type in exactMatches)
                    {
                        services.AddTransient(@interface, type);
                    }
                }
                else
                {
                    if (exactMatches.Count > 1)
                    {
                        exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
                    }

                    foreach (var type in exactMatches)
                    {
                        services.TryAddTransient(@interface, type);
                    }
                }

                if (!@interface.IsOpenGeneric())
                {
                    AddConcretionsThatCouldBeClosed(@interface, concretions, services);
                }
            }
        }

        private static bool IsMatchingWithInterface(Type handlerType, Type handlerInterface)
        {
            if (handlerType == null || handlerInterface == null)
            {
                return false;
            }

            if (handlerType.IsInterface)
            {
                if (handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments))
                {
                    return true;
                }
            }
            else
            {
                return IsMatchingWithInterface(handlerType.GetInterface(handlerInterface.Name), handlerInterface);
            }

            return false;
        }

        private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
        {
            foreach (var type in concretions.Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
            {
                try
                {
                    services.TryAddTransient(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        internal static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
        {
            var openInterface = closedInterface.GetGenericTypeDefinition();
            var arguments = closedInterface.GenericTypeArguments;

            var concreteArguments = openConcretion.GenericTypeArguments;
            return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
        }

        private static bool CanBeCastTo(this Type pluggedType, Type pluginType)
        {
            if (pluggedType == null)
            {
                return false;
            }

            if (pluggedType == pluginType)
            {
                return true;
            }

            return pluginType.IsAssignableFrom(pluggedType);
        }

        private static bool IsOpenGeneric(this Type type)
        {
            return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
        }

        internal static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
        {
            return FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();
        }

        private static IEnumerable<Type> FindInterfacesThatClosesCore(Type pluggedType, Type templateType)
        {
            if (pluggedType == null)
            {
                yield break;
            }

            if (!pluggedType.IsConcrete())
            {
                yield break;
            }

            if (templateType.IsInterface)
            {
                foreach (
                    var interfaceType in
                    pluggedType.GetInterfaces()
                        .Where(type => type.IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
                {
                    yield return interfaceType;
                }
            }
            else if (pluggedType.BaseType.IsGenericType &&
                     (pluggedType.BaseType.GetGenericTypeDefinition() == templateType))
            {
                yield return pluggedType.BaseType;
            }

            if (pluggedType.BaseType == typeof(object))
            {
                yield break;
            }

            foreach (var interfaceType in FindInterfacesThatClosesCore(pluggedType.BaseType, templateType))
            {
                yield return interfaceType;
            }
        }

        private static bool IsConcrete(this Type type)
        {
            return !type.IsAbstract && !type.IsInterface;
        }

        private static void Fill<T>(this IList<T> list, T value)
        {
            if (list.Contains(value))
            {
                return;
            }

            list.Add(value);
        }

        public static void AddRequiredServices(IServiceCollection services, MediatorServiceConfiguration serviceConfiguration)
        {
            services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfiguration.MediatorImplementationType, serviceConfiguration.Lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));
            services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));

            var notificationPublisherServiceDescriptor = serviceConfiguration.NotificationPublisherType != null
                ? new ServiceDescriptor(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisherType, serviceConfiguration.Lifetime)
                : new ServiceDescriptor(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisher);

            services.TryAdd(notificationPublisherServiceDescriptor);

            if (serviceConfiguration.RequestExceptionActionProcessorStrategy == RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions)
            {
                RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
                RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
            }
            else
            {
                RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
                RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
            }

            if (serviceConfiguration.RequestPreProcessorsToRegister.Any())
            {
                services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), ServiceLifetime.Transient));
                services.TryAddEnumerable(serviceConfiguration.RequestPreProcessorsToRegister);
            }

            if (serviceConfiguration.RequestPostProcessorsToRegister.Any())
            {
                services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>), ServiceLifetime.Transient));
                services.TryAddEnumerable(serviceConfiguration.RequestPostProcessorsToRegister);
            }

            foreach (var serviceDescriptor in serviceConfiguration.BehaviorsToRegister)
            {
                services.TryAddEnumerable(serviceDescriptor);
            }

            foreach (var serviceDescriptor in serviceConfiguration.StreamBehaviorsToRegister)
            {
                services.TryAddEnumerable(serviceDescriptor);
            }
        }

        private static void RegisterBehaviorIfImplementationsExist(IServiceCollection services, Type behaviorType, Type subBehaviorType)
        {
            var hasAnyRegistrationsOfSubBehaviorType = services
                .Where(service => !service.IsKeyedService)
                .Select(service => service.ImplementationType)
                .OfType<Type>()
                .SelectMany(type => type.GetInterfaces())
                .Where(type => type.IsGenericType)
                .Select(type => type.GetGenericTypeDefinition())
                .Any(type => type == subBehaviorType);

            if (hasAnyRegistrationsOfSubBehaviorType)
            {
                services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, ServiceLifetime.Transient));
            }
        }
    }
}