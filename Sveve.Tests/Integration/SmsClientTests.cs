namespace Sveve.Tests.Integration;

public class SmsClientTests : IDisposable
{
    private const string Sender = "SveveDotnet";
    private static readonly TestPerson PersonA = new("Line Danser", "99999999");
    private static readonly TestPerson PersonB = new("Roland Gundersen", "44444444");

    private readonly SveveClient _client = new(new()
    {
        Username = TestEnvironment.Configuration["SVEVE:USERNAME"]!,
        Password = TestEnvironment.Configuration["SVEVE:PASSWORD"]!,
        IsTest = true
    });

    [Fact]
    public async Task SendSingleSms()
    {
        var success = await _client.Sms.SendSingleAsync(new SendSmsRequest(PersonA.PhoneNumber, "Dette er en test")
        {
            Sender = Sender,
            IsTest = true
        });

        Assert.NotNull(success);
        Assert.True(success.IsSuccess);
        Assert.True(success.MessageId > 0);
        Assert.Null(success.Error);

        var notANumber = await _client.Sms.SendSingleAsync(new SendSmsRequest("not a phone number", "Dette er en test") { Sender = Sender });

        Assert.NotNull(notANumber);
        Assert.False(notANumber.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => notANumber.MessageId);
        Assert.Equal("Telefonnummeret kan bare inneholde tall", notANumber.Error);

        var notAMobileNumber = await _client.Sms.SendSingleAsync(new SendSmsRequest("12345678", "Dette er en test")
        {
            Sender = Sender,
            IsTest = true
        });

        Assert.NotNull(notAMobileNumber);
        Assert.False(notAMobileNumber.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => notAMobileNumber.MessageId);
        Assert.Equal("Telefonnummeret er ikke et mobilnummer", notAMobileNumber.Error);
    }

    [Fact]
    public async Task SendManyAsync()
    {
        var success = new SendSmsRequest(PersonA.PhoneNumber, "Dette er en test");
        var notAMobileNumber = new SendSmsRequest("12345678", "Dette er en test");

        var results = await _client.Sms.SendBulkAsync([success, notAMobileNumber]);

        var successResult = results.FirstOrDefault(x => x.ReceiverPhoneNumber == PersonA.PhoneNumber);
        Assert.NotNull(successResult);
        Assert.True(successResult.IsSuccess);

        var notAMobileNumberResult = results.FirstOrDefault(x => x.ReceiverPhoneNumber == "12345678");
        Assert.NotNull(notAMobileNumberResult);
        Assert.False(notAMobileNumberResult.IsSuccess);
    }

    [Fact]
    public async Task SendManyAsync_ThrowsIfMixOfTest()
    {
        var realRequest = new SendSmsRequest(PersonA.PhoneNumber, "Dette er ikke en test");
        var testRequest = new SendSmsRequest(PersonB.PhoneNumber, "Dette er en test") { IsTest = true };

        await Assert.ThrowsAsync<ArgumentException>(() => _client.Sms.SendBulkAsync([realRequest, testRequest]));
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    private record TestPerson(string Name, string PhoneNumber);
}