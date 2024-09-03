# Sveve.Net

![Sveve logo](./docs/logo-sveve.svg)

A dotnet client written in C# for the REST-APIs provided by [Sveve](#https://sveve.no/).

## Send a SMS
A message can be created by instantiating a `SendSmsRequest` object.
```cs
var receiver = "98765432";
var text = "The actual content of the SMS";
var request = new SendSmsRequest(receiver, text);

// Send the SMS
SveveClient client = ...;
await client.Sms.SendAsync(request, cancellationToken);
```

If you want to send a single SMS to a few receivers, you can specify multiple receivers separated by a comma:
```cs
// Can be a phone number, recipient group or list of both.
var text = "...";
var request = new SendSmsRequest(receivers, text);

// Send the SMS
SveveClient client = ...;
await client.Sms.SendAsync(request, cancellationToken);
```

If the list of receivers grow very large, you might want to look into [receiver groups](#manage-recipient-groups)

## Advanced SMS sending
The `SendSmsRequest` can be further configured:

### Spesify sender
To specify the sender, add a `Sender` property to either the `SveveClientOptions`:
```cs
var client = new SveveClient(new SveveClientOptions
{
    Sender = "My company"
    
    // other properties ...
});
```
or to the `SendSmsRequest` itself:
```cs
var request = new SendSmsRequest(receiver, text)
{
    Sender = "My company"
};
```

If `Sender` is specified in both `SveveClientOptions` and `SendSmsRequest`, `SendSmsRequest.Sender` is used.

### Repeat a SMS
The `SendSmsRequest.Repeat` property can be used to configure repetition of a SMS:
```cs
SendSmsRequest request = ...;

// Repeat message every 2 days until message has been sent three times
request.Repeat = SmsRepetition
    .Daily(days: 2)
    .EndsAfter(repeatedCount: 3);

// Repeat message once a week for two months.
request.Repeat = SmsRepetition
    .Weekly()
    .EndsOn(DateTime.Today.AddMonths(2));
```
See the `SmsRepetition` class for more.

## Manage recipient groups

Implemented, but documentation is comming...

## Manage account
Request more SMS units
```cs
SveveClient client = ...;

// Order 400 additional SMS units.
await client.Admin.OrderSmsAsync(count:400);
```

Check remaining SMS units
```cs
SveveClient client = ...;

var smsCount = await client.Admin.RemainingSmsAsync();
if (smsCount < 400)
{
    // Do stuff if less than 400
    // remaining SMS units.
}
```