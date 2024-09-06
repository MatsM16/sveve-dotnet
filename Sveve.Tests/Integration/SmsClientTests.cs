using System.Security.Authentication;

namespace Sveve.Tests.Integration;

public class SmsClientTests : IDisposable
{
    private static readonly TestPerson PersonA = new("Line Danser", "99999999");

    private readonly SveveClient _client = new(new()
    {
        Username = TestEnvironment.Configuration["SVEVE:USERNAME"]!,
        Password = TestEnvironment.Configuration["SVEVE:PASSWORD"]!,
        IsTest = true
    });

    [Fact]
    public async Task SendSingleAsync()
    {
        var messageId = await _client.Sms.SendSingleAsync(PersonA.PhoneNumber, "Dette er en test");
        Assert.True(messageId > 0);
    }

    [Fact]
    public async Task SendSingleAsync_ThrowsIfNotSingleMobilePhoneNumber()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _client.Sms.SendSingleAsync("my group name", "A group name"));
        await Assert.ThrowsAsync<ArgumentException>(() => _client.Sms.SendSingleAsync("411111111,411111111", "Not a single number"));
        await Assert.ThrowsAsync<SmsNotSentException>(() => _client.Sms.SendSingleAsync("12345678", "Not a mobile number"));
    }

    [Fact]
    public async Task SendSingleAsync_ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid",
            IsTest = true
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Sms.SendSingleAsync(PersonA.PhoneNumber, "Dette er en test"));
    }

    [Fact]
    public async Task SendAsync()
    {
        var results = await _client.Sms.SendAsync($"{PersonA.PhoneNumber},12345678", "Dette er en test");
        Assert.True(results.Count is 2);

        var success = results.FirstOrDefault(x => x.ReceiverPhoneNumber == PersonA.PhoneNumber);
        Assert.NotNull(success);
        Assert.True(success.IsSentSuccessfully);

        var failed = results.FirstOrDefault(x => x.ReceiverPhoneNumber == "12345678");
        Assert.NotNull(failed);
        Assert.False(failed.IsSentSuccessfully);
        Assert.Equal("Telefonnummeret er ikke et mobilnummer", failed.Error);
    }

    [Fact]
    public async Task SendAsync_VerifiesParameters()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.Sms.SendAsync(null!, "Dette er en test"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.Sms.SendAsync("", "Dette er en test"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.Sms.SendAsync(" ", "Dette er en test"));

        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.Sms.SendAsync(PersonA.PhoneNumber, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.Sms.SendAsync(PersonA.PhoneNumber, ""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.Sms.SendAsync(PersonA.PhoneNumber, " "));
    }

    [Fact]
    public async Task SendAsync_ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid",
            IsTest = true
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Sms.SendAsync(PersonA.PhoneNumber, "Dette er en test"));
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    private record TestPerson(string Name, string PhoneNumber);
}