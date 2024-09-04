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
            IsTest = true
        });

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<SveveClient>());
        Assert.NotNull(provider.GetService<SveveSmsClient>());
        Assert.NotNull(provider.GetService<SveveAdminClient>());
        Assert.NotNull(provider.GetService<SveveGroupClient>());
    }
}
