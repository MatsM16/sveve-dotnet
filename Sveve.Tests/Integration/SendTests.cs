using System.Security.Authentication;

namespace Sveve.Tests.Integration;

public class SendTests : IDisposable
{
    private static readonly TestPerson PersonA = new("Line Danser", "99999999");

    private readonly SveveClient _client = new(new()
    {
        Username = TestEnvironment.Configuration["SVEVE:USERNAME"]!,
        Password = TestEnvironment.Configuration["SVEVE:PASSWORD"]!,
        Test = true
    });

    [Fact]
    public async Task SendAsync()
    {
        var response = await _client.SendAsync(new Sms($"{PersonA.PhoneNumber},12345678", "Dette er en test"));
        Assert.True(response.MessageId(PersonA.PhoneNumber) > 0);
        var error = Assert.Single(response.Errors);
        Assert.Equal("12345678", error.PhoneNumber);
        Assert.Equal("Telefonnummeret er ikke et mobilnummer", error.Reason);
    }

    [Fact]
    public async Task SendAsync_ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid",
            Test = true
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.SendAsync(new Sms(PersonA.PhoneNumber, "Dette er en test")));
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    private record TestPerson(string Name, string PhoneNumber);
}