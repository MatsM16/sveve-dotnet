using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Sveve.Tests;

public static class TestEnvironment
{
    public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddOptionalJsonResource("Sveve.Tests.appsettings.local.json")
        .Build();
}

file static class Extensions
{
    public static IConfigurationBuilder AddOptionalJsonResource(this IConfigurationBuilder builder, string resourceName) => 
        builder.AddOptionalJsonStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName));

    public static IConfigurationBuilder AddOptionalJsonStream(this IConfigurationBuilder builder, Stream? stream) => 
        stream is not null ? builder.AddJsonStream(stream) : builder;
}
