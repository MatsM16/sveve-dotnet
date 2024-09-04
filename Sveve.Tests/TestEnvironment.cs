using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Sveve.Tests;

public static class TestEnvironment
{
    public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddAppSettings()
        .Build();
}

file static class Extensions
{
    /// <summary>
    /// Adds appsettings.*.json files from Sveve.Tests to the configuration.
    /// </summary>
    public static IConfigurationBuilder AddAppSettings(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resourceName in assembly.GetManifestResourceNames())
            if (resourceName.StartsWith("Sveve.Tests.appsettings.") && resourceName.EndsWith(".json"))
                builder.AddJsonStream(assembly.GetManifestResourceStream(resourceName)!);
        return builder;
    }
}
