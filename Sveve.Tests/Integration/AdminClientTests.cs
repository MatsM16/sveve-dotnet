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

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}