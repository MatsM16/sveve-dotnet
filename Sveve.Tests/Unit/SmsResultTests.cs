namespace Sveve.Tests.Unit;

public class SmsResultTests
{
    [Fact]
    public void ReceiverIsSinglePhoneNumber()
    {
        Assert.NotNull(SmsResult.Ok("1234", 1));
        Assert.NotNull(SmsResult.Failed("1234", "Reason"));

        Assert.Throws<ArgumentNullException>(() => SmsResult.Ok(null!, 1));
        Assert.Throws<ArgumentNullException>(() => SmsResult.Ok("", 1));
        Assert.Throws<ArgumentException>(() => SmsResult.Ok("not a number", 1));
        Assert.Throws<ArgumentException>(() => SmsResult.Ok("1234,1234", 1));

        Assert.Throws<ArgumentNullException>(() => SmsResult.Failed(null!, "Reason"));
        Assert.Throws<ArgumentNullException>(() => SmsResult.Failed("", "Reason"));
        Assert.Throws<ArgumentException>(() => SmsResult.Failed("not a number", "Reason"));
        Assert.Throws<ArgumentException>(() => SmsResult.Failed("1234,1234", "Reason"));
    }

    [Fact]
    public void IsTest()
    {
        Assert.False(SmsResult.Ok("1234", 1).IsTest);
        Assert.False(SmsResult.Ok("1234", 1).IsTest);
        Assert.True(SmsResult.Ok("1234", 1, true).IsTest);
        Assert.False(SmsResult.Ok("1234", 1, false).IsTest);
        Assert.True(SmsResult.Failed("1234", "Reason", true).IsTest);
        Assert.False(SmsResult.Failed("1234", "Reason", false).IsTest);
    }

    [Fact]
    public void Ok()
    {
        var result = SmsResult.Ok("1234", 1);
        Assert.True(result.IsSentSuccessfully);
        Assert.Null(result.Error);
        Assert.Equal(1, result.MessageId);
    }

    [Fact]
    public void Ok_MessageIdIsPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SmsResult.Ok("1234", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SmsResult.Ok("1234", -1));
        Assert.NotNull(SmsResult.Ok("1234", 1));
    }

    [Fact]
    public void Failed()
    {
        var result = SmsResult.Failed("1234", "Reason");
        Assert.False(result.IsSentSuccessfully);
        Assert.Equal("Reason", result.Error);
        Assert.Throws<SmsNotSentException>(() => result.MessageId);
    }

    [Fact]
    public void Failed_ErrorAllowsNullAndEmpty()
    {
        Assert.NotNull(SmsResult.Failed("1234", null));
        Assert.NotNull(SmsResult.Failed("1234", ""));
        Assert.NotNull(SmsResult.Failed("1234", " "));
    }
}
