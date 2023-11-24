using Colosoft.Mediator;
using Colosoft.Mediator.NotificationPublishers;
using Colosoft.Mediator.Pipeline;
using Colosoft.Mediator.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public class MediatorServiceConfiguration
    {
        public Func<Type, bool> TypeEvaluator { get; set; } = t => true;

        public Type MediatorImplementationType { get; set; } = typeof(Mediator);

        public INotificationPublisher NotificationPublisher { get; set; } = new ForeachAwaitPublisher();

        public Type NotificationPublisherType { get; set; }

        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

        public RequestExceptionActionProcessorStrategy RequestExceptionActionProcessorStrategy { get; set; }
            = RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions;

        internal List<Assembly> AssembliesToRegister { get; } = new List<Assembly>();

        public List<ServiceDescriptor> BehaviorsToRegister { get; } = new List<ServiceDescriptor>();

        public List<ServiceDescriptor> StreamBehaviorsToRegister { get; } = new List<ServiceDescriptor>();

        public List<ServiceDescriptor> RequestPreProcessorsToRegister { get; } = new List<ServiceDescriptor>();

        public List<ServiceDescriptor> RequestPostProcessorsToRegister { get; } = new List<ServiceDescriptor>();

        public bool AutoRegisterRequestProcessors { get; set; }

        public MediatorServiceConfiguration RegisterServicesFromAssemblyContaining<T>() =>
            this.RegisterServicesFromAssemblyContaining(typeof(T));

        public MediatorServiceConfiguration RegisterServicesFromAssemblyContaining(Type type) =>
            this.RegisterServicesFromAssembly(type.Assembly);

        public MediatorServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
        {
            this.AssembliesToRegister.Add(assembly);
            return this;
        }

        public MediatorServiceConfiguration RegisterServicesFromAssemblies(
            params Assembly[] assemblies)
        {
            this.AssembliesToRegister.AddRange(assemblies);
            return this;
        }

        public MediatorServiceConfiguration AddBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient) =>
            this.AddBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

        public MediatorServiceConfiguration AddBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            return this.AddBehavior(typeof(TImplementationType), serviceLifetime);
        }

        public MediatorServiceConfiguration AddBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IPipelineBehavior<,>)).ToList();

            if (implementedGenericInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
            }

            foreach (var implementedBehaviorType in implementedGenericInterfaces)
            {
                this.BehaviorsToRegister.Add(new ServiceDescriptor(implementedBehaviorType, implementationType, serviceLifetime));
            }

            return this;
        }

        public MediatorServiceConfiguration AddBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            this.BehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
            return this;
        }

        public MediatorServiceConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            if (!openBehaviorType.IsGenericType)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
            }

            var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
            var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IPipelineBehavior<,>)));

            if (implementedOpenBehaviorInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
            }

            foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
            {
                this.BehaviorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
            }

            return this;
        }

        public MediatorServiceConfiguration AddStreamBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient) =>
            this.AddStreamBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

        public MediatorServiceConfiguration AddStreamBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            this.StreamBehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

            return this;
        }

        public MediatorServiceConfiguration AddStreamBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient) =>
            this.AddStreamBehavior(typeof(TImplementationType), serviceLifetime);

        public MediatorServiceConfiguration AddStreamBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IStreamPipelineBehavior<,>)).ToList();

            if (implementedGenericInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IStreamPipelineBehavior<,>).FullName}");
            }

            foreach (var implementedBehaviorType in implementedGenericInterfaces)
            {
                this.StreamBehaviorsToRegister.Add(new ServiceDescriptor(implementedBehaviorType, implementationType, serviceLifetime));
            }

            return this;
        }

        public MediatorServiceConfiguration AddOpenStreamBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            if (!openBehaviorType.IsGenericType)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
            }

            var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
            var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IStreamPipelineBehavior<,>)));

            if (implementedOpenBehaviorInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IStreamPipelineBehavior<,>).FullName}");
            }

            foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
            {
                this.StreamBehaviorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
            }

            return this;
        }

        public MediatorServiceConfiguration AddRequestPreProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient) =>
            this.AddRequestPreProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

        public MediatorServiceConfiguration AddRequestPreProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            this.RequestPreProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

            return this;
        }

        public MediatorServiceConfiguration AddRequestPreProcessor<TImplementationType>(
            ServiceLifetime serviceLifetime = ServiceLifetime.Transient) =>
            this.AddRequestPreProcessor(typeof(TImplementationType), serviceLifetime);

        public MediatorServiceConfiguration AddRequestPreProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPreProcessor<>)).ToList();

            if (implementedGenericInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
            }

            foreach (var implementedPreProcessorType in implementedGenericInterfaces)
            {
                this.RequestPreProcessorsToRegister.Add(new ServiceDescriptor(implementedPreProcessorType, implementationType, serviceLifetime));
            }

            return this;
        }

        public MediatorServiceConfiguration AddOpenRequestPreProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            if (!openBehaviorType.IsGenericType)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
            }

            var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
            var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPreProcessor<>)));

            if (implementedOpenBehaviorInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
            }

            foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
            {
                this.RequestPreProcessorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
            }

            return this;
        }

        public MediatorServiceConfiguration AddRequestPostProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient) =>
            this.AddRequestPostProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

        public MediatorServiceConfiguration AddRequestPostProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            this.RequestPostProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));
            return this;
        }

        public MediatorServiceConfiguration AddRequestPostProcessor<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient) =>
            this.AddRequestPostProcessor(typeof(TImplementationType), serviceLifetime);

        public MediatorServiceConfiguration AddRequestPostProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPostProcessor<,>)).ToList();

            if (implementedGenericInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
            }

            foreach (var implementedPostProcessorType in implementedGenericInterfaces)
            {
                this.RequestPostProcessorsToRegister.Add(new ServiceDescriptor(implementedPostProcessorType, implementationType, serviceLifetime));
            }

            return this;
        }

        public MediatorServiceConfiguration AddOpenRequestPostProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            if (!openBehaviorType.IsGenericType)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
            }

            var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
            var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPostProcessor<,>)));

            if (implementedOpenBehaviorInterfaces.Count == 0)
            {
                throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
            }

            foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
            {
                this.RequestPostProcessorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
            }

            return this;
        }
    }
}