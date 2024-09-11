namespace Sveve.Tests.Unit;

public class SmsRecipientTests
{
    [Fact]
    public void IdentifiesPhoneNumbers()
    {
        Assert.True(new SmsRecipient("+4799999999").IsPhoneNumber);
        Assert.True(new SmsRecipient("004799999999").IsPhoneNumber);
        Assert.True(new SmsRecipient("99999999").IsPhoneNumber);
        Assert.True(new SmsRecipient("999 99 999").IsPhoneNumber);
        Assert.False(new SmsRecipient("My group").IsPhoneNumber);
    }
    
    [Fact]
    public void NormalizesPhoneNumbers()
    {
        List<SmsRecipient> recipients = 
        [
            new ("+4799999999"),
            new ("004799999999"),
            new ("99999999"),
            new (" 99999999 "),
            new ("999 99 999")
        ];
        
        Assert.All(recipients, r => 
        {
            Assert.True(r.IsPhoneNumber);
            Assert.Equal(r, recipients[0]);
            Assert.True(r == recipients[0]);
        });

        Assert.Equal("99999999", new SmsRecipient("+4799999999").ToString());
        Assert.Equal("+199999999", new SmsRecipient("+199999999").ToString());
        Assert.Equal("+199999999", new SmsRecipient("00199999999").ToString());
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyRecipient()
    {
        Assert.Throws<ArgumentNullException>(() => new SmsRecipient(null!));
        Assert.Throws<ArgumentNullException>(() => new SmsRecipient(""));
        Assert.Throws<ArgumentNullException>(() => new SmsRecipient(" "));
    }

    [Fact]
    public void Constructor_ThrowsIfMultipleRecipients()
    {
        Assert.Throws<ArgumentException>(() => new SmsRecipient("+4799999999, +4799999999"));
    }

    [Fact]
    public void Multiple_ThrowsIfNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => SmsRecipient.Multiple(null!));
        Assert.Throws<ArgumentNullException>(() => SmsRecipient.Multiple(""));
        Assert.Throws<ArgumentNullException>(() => SmsRecipient.Multiple(" "));
    }

    [Fact]
    public void Multiple_ThrowsIfAnyRecipientIsInvalid()
    {
        Assert.Throws<ArgumentNullException>(() => SmsRecipient.Multiple("+4799999999, "));
    }

    [Fact]
    public void Multiple()
    {
        Assert.Equal(2, SmsRecipient.Multiple("+4799999999, +4799999999").Count);
    }

    [Fact]
    public void IsPhoneNumberInternal()
    {
        Assert.True(SmsRecipient.IsPhoneNumberInternal("+4799999999"));
        Assert.True(SmsRecipient.IsPhoneNumberInternal("004799999999"));
        Assert.True(SmsRecipient.IsPhoneNumberInternal("99999999"));
        Assert.True(SmsRecipient.IsPhoneNumberInternal("999 99 999"));

        Assert.False(SmsRecipient.IsPhoneNumberInternal(null!));
        Assert.False(SmsRecipient.IsPhoneNumberInternal(""));
        Assert.False(SmsRecipient.IsPhoneNumberInternal(" "));
        Assert.False(SmsRecipient.IsPhoneNumberInternal("My group"));
    }
}
