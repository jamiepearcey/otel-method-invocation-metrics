using Castle.DynamicProxy;
using MetricsLibrary.Interceptors;
using MetricsLibrary.Options;
using Microsoft.Extensions.DependencyInjection;
#if NET47
using System;
#endif

namespace MetricsLibrary.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMetricsInterceptor<T>(
        this IServiceCollection services,
        Action<MetricsInterceptorOptions<T>>? configureOptions = null) 
        where T : class
    {
        var options = new MetricsInterceptorOptions<T>();
        configureOptions?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<IProxyGenerator, ProxyGenerator>();
        services.AddSingleton<MetricsInterceptor<T>>();
        
        return services;
    }

    public static IServiceCollection AddProxiedService<TInterface, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        
        services.Add(new ServiceDescriptor(
            typeof(TInterface),
            sp =>
            {
                var proxyGenerator = sp.GetRequiredService<IProxyGenerator>();
                var implementation = sp.GetRequiredService<TImplementation>();
                var interceptor = sp.GetRequiredService<MetricsInterceptor<TInterface>>();
                
                return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(
                    implementation,
                    interceptor);
            },
            lifetime));
            
        return services;
    }
} 