using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Sveve.Extensions;

/// <summary>
/// Extension methods to help register <see cref="SveveClient"/> on the service container.
/// </summary>
public static class SveveServiceCollectionExtensions
{
    /// <inheritdoc cref="AddSveveClient(IServiceCollection, Func{IServiceProvider, SveveClientOptions})" />
    /// <param name="options"></param>
    /// <param name="services"></param>
    public static IServiceCollection AddSveveClient(this IServiceCollection services, SveveClientOptions options)
    {
        return services.AddSveveClient(_ => options);
    }

    /// <summary>
    /// Registers the <see cref="SveveClient"/> and required services.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionsFactory"></param>
    /// <returns> <paramref name="services"/> </returns>
    public static IServiceCollection AddSveveClient(this IServiceCollection services, Func<IServiceProvider, SveveClientOptions> optionsFactory)
    {
        services.AddSingleton(optionsFactory);
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<SveveClientOptions>();

            options.LoggerFactory ??= sp.GetService<ILoggerFactory>();


            return new SveveClient(options);
        });
        return services;
    }
}
