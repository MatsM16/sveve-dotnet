using Microsoft.Extensions.DependencyInjection;

namespace Sveve.Tests.Unit;

public class ServiceCollectionTests
{
    [Fact]
    public void AddsSveveServices()
    {
        var services = new ServiceCollection();

        services.AddSveveClient(options => new()
        {
            Username = "username",
            Password = "password",
            IsTest = true
        });

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<SveveClient>());
        Assert.NotNull(provider.GetService<SveveSmsClient>());
        Assert.NotNull(provider.GetService<SveveAdminClient>());
        Assert.NotNull(provider.GetService<SveveGroupClient>());
    }
}
