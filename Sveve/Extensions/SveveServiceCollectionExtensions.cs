using Microsoft.Extensions.DependencyInjection;
using System;

namespace Sveve.Extensions;

/// <summary>
/// Extension methods to help register <see cref="SveveClient"/> on the service container.
/// </summary>
public static class SveveServiceCollectionExtensions
{
    /// <inheritdoc cref="AddSveveClient(IServiceCollection, Func{IServiceProvider, SveveClientOptions})" />
    /// <param name="options"></param>
    public static IServiceCollection AddSveveClient(this IServiceCollection services, SveveClientOptions options)
    {
        return services.AddSveveClient(_ => options);
    }

    /// <summary>
    /// Registers the <see cref="SveveClient"/> and required services.
    /// </summary>
    /// <remarks>
    /// Registers:<br/>
    /// - <see cref="SveveClient"/> (singleton)<br/>
    /// - <see cref="SveveSmsClient"/> (singleton)<br/>
    /// - <see cref="SveveGroupClient"/> (singleton)<br/>
    /// - <see cref="SveveAdminClient"/> (singleton)<br/>
    /// </remarks>
    /// <param name="services"></param>
    /// <param name="optionsFactory"></param>
    /// <returns> <paramref name="services"/> </returns>
    public static IServiceCollection AddSveveClient(this IServiceCollection services, Func<IServiceProvider, SveveClientOptions> optionsFactory)
    {
        services.AddSingleton(optionsFactory);
        services.AddSingleton(sp => new SveveClient(sp.GetRequiredService<SveveClientOptions>()));
        services.AddSingleton(sp => sp.GetRequiredService<SveveClient>().Sms);
        services.AddSingleton(sp => sp.GetRequiredService<SveveClient>().Admin);
        services.AddSingleton(sp => sp.GetRequiredService<SveveClient>().Groups);
        return services;
    }
}
