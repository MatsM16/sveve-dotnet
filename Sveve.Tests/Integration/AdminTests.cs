using System.Security.Authentication;

namespace Sveve.Tests.Integration;

public class AdminTests : IDisposable
{
    private readonly SveveClient _client = new(new()
    {
        Username = TestEnvironment.Configuration["SVEVE:USERNAME"]!,
        Password = TestEnvironment.Configuration["SVEVE:PASSWORD"]!,
        Test = true
    });

    [Fact]
    public async Task RemainingSms()
    {
        var remainingSmsUnits = await _client.RemainingSmsUnitsAsync();
        Assert.True(remainingSmsUnits > 0, "The configured account has no remaining SMS units");
    }

    [Fact]
    public async Task ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid"
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.PurchaseSmsUnitsAsync(SmsUnitOrder.Bulk500));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.RemainingSmsUnitsAsync());
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}