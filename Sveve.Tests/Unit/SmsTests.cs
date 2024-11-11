namespace Sveve.Tests.Unit;

public class SmsTests
{
    [Fact]
    public void ToIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new Sms(null!, "Dette er en test"));
        Assert.Throws<ArgumentNullException>(() => new Sms("", "Dette er en test"));
        Assert.Throws<ArgumentNullException>(() => new Sms(" ", "Dette er en test"));
    }

    [Fact]
    public void MessageIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new Sms("12345678", null!));
        Assert.Throws<ArgumentNullException>(() => new Sms("12345678", ""));
        Assert.Throws<ArgumentNullException>(() => new Sms("12345678", " "));
    }
}
