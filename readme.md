# Sveve.Net

![Sveve logo](./docs/logo.svg)

A dotnet client written in C# for the REST-APIs provided by [Sveve](https://sveve.no/).  
**Note:** this is an unofficial library and not made or supported by Sveve.
  
- [Get started](#get-started)
- [Send a SMS](#send-a-sms)
- [Configure SMS sending](#configure-sms-sending)
- [Manage recipient groups](#manage-recipient-groups)
- [Manage account](#manage-account)
- [Preview builds](#preview-builds)
- [Get inspired](https://sveve.no/artikler/inspirasjon) (_Norwegian only_)
- [Sveve API documentation](https://sveve.no/apidok/)

## Get started
You can either create a `SveveClient` directly:
```cs
var client = new SveveClient(new SveveClientOptions
{
    Username = "MyCompanyUsername",
    Password = "super_secret_password";
});
```
or using the `IServiceCollection` extension:
```cs
services.AddSveveClient(new SveveClientOptions { ... });
```
Using the extension method registers a `SveveClient`, `SveveSmsClient`, `SveveGroupClient`, `SveveAdminClient` and the `SveveClientOptions`.  
All clients are registered as singletons.  

## Send a SMS
```cs
// Send SMS to a single phone number
await client.Sms.SendAsync("98765432", "The actual content of the SMS");

// Send SMS to two spesific phone numbers
await client.Sms.SendAsync("98765432,90876543", "Drink water!");

// Send SMS to a named group
await client.Sms.SendAsync("people-who-dont-drink-enough-water", "Drink more water!");
```
To manage the named groups, see [receiver groups](#manage-recipient-groups)  
  
`SveveSmsClient.SendAsync` returns a list of `SmsResult`-objects which contains information about the sending of each individual sms.  
If you only intend to send the SMS to a single receiver, you could use:
```cs
var messageId = await client.Sms.SendSingleAsync("98765432", "The actual content of the SMS");
```
`SendSingleAsync` throws if sending fails and returns the message-id from Sveve directly.

## SMS Options
When sending a SMS, you can pass a `SmsOptions`-object to alter how the SMS is sent.
```cs
var options = new SmsOptions
{
    ...
}
await client.Sms.SendAsync("98765432", "Some text message...", options);
```

### Custom sender
To specify the sender, add a `Sender` property to either the `SveveClientOptions`:
```cs
var client = new SveveClient(new SveveClientOptions
{
    Sender = "My company"
    
    // other properties ...
});
```
or to the `SmsOptions` itself:
```cs
var options = new SmsOptions
{
    Sender = "My company"
};
```

If `Sender` is specified in both `SveveClientOptions` and `SmsOptions`, `SmsOptions.Sender` is used.  
When a receiver sees your message, the `Sender` will be used as a display name.

### Repeat a SMS
The `SmsOptions.Repeat` property can be used to configure repetition of a SMS:
```cs
var options = new SmsOptions();

// Repeat message every 2 days until message has been sent three times
options.Repeat = SmsRepetition
    .Daily(days: 2)
    .EndsAfter(repeatedCount: 3);

// Repeat message once a week for two months.
options.Repeat = SmsRepetition
    .Weekly()
    .EndsOn(DateTime.Today.AddMonths(2));
```
See the `SmsRepetition` class for more.

### Schedule a SMS
The `SmsOptions.ScheduledSendTime` configures when the message will be sent.  
If the provided `DateTimeOffset` is earier than now, it is ignored.

```cs
await client.Sms.SendAsync("vip-customers", "Use promocode vip123 for special offer", new SmsOptions
{
    ScheduledSendTime = vipPromo.StartDate.AddDays(-1)
});
```

### Test a message
To test sending messages, add `IsTest=true` to either `SveveClientOptions` or `SmsOptions`.  
If `SveveClientOptions.IsTest` is `true`, all messages will be sent as test messages regardless of the `SmsOptions.IsTest` value.

## Manage recipient groups
Groups can be a useful way to reach large amounts of people with the same SMS.  
**Note:** You will be charged for each individual SMS.

A group is created either by explicitly calling
```cs
await client.Group.CreateAsync("my-group");
```
or by adding the first receiver
```cs
await client.Group.AddRecipientAsync("my-group", "Some Person", "12345678");
```

You can now send an sms to every member of a group by calling the normal send methods with the group name as the recipient.
```cs
await client.Sms.SendAsync("my-group", "Super-sale! Only on monday!")
```

Once you are done using the group, it can be deleted by calling
```cs
await client.Group.DeleteAsync("my-group");
```

## Manage account
Request more SMS units
```cs
// Order 500 additional SMS units.
await client.Admin.OrderSmsAsync(SmsOrderSize.Bulk500);
```

Check remaining SMS units
```cs
var smsCount = await client.Admin.RemainingSmsAsync();
if (smsCount < 500)
{
    // Do stuff if less than 500
    // remaining SMS units.
}
```

# Preview builds
If you want the preview builds add the NuGet source: `https://nuget.pkg.github.com/MatsM16/index.json`.  
It should be noted that these builds are considered unstable at best and non-functional at worst.  
Use at you own risk.