using System.Security.Authentication;

namespace Sveve.Tests.Integration;

public class AdminClientTests : IDisposable
{
    private readonly SveveClient _client = new(new()
    {
        Username = TestEnvironment.Configuration["SVEVE:USERNAME"]!,
        Password = TestEnvironment.Configuration["SVEVE:PASSWORD"]!,
        IsTest = true
    });

    [Fact]
    public async Task RemainingSms()
    {
        var remainingSmsUnits = await _client.Admin.RemainingSmsAsync();
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

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Admin.OrderSmsAsync(SmsOrderSize.Bulk500));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Admin.RemainingSmsAsync());
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}