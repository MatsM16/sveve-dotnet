using Microsoft.Extensions.DependencyInjection;
using Sveve.Extensions;

namespace Sveve.Tests.Unit;

public class ServiceCollectionTests
{
    [Fact]
    public void AddSveveClient()
    {
        var services = new ServiceCollection();

        services.AddSveveClient(options => new()
        {
            Username = "username",
            Password = "password",
            Test = true
        });

        var provider = services.BuildServiceProvider();

        AssertSingleton<SveveClientOptions>(provider);
        AssertSingleton<SveveClient>(provider);
    }

    private static void AssertSingleton<TService>(IServiceProvider provider)
    {
        var instance1 = provider.GetService<TService>();
        var instance2 = provider.GetService<TService>();

        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Equal(instance1, instance2);
    }
}
