using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sveve.AspNetCore;
using System.Net;

namespace Sveve.Tests.Unit;

public class SveveConsumerTests
{
    public const string Number = "12345678";
    public const string Msg = "Some message";

    [Fact]
    public async Task DetectMissingNumber()
    {
        AssertBadRequest("number", await SveveEndpoint.Endpoint(null!, null!));
        AssertBadRequest("number", await SveveEndpoint.Endpoint(null!, ""));
        AssertBadRequest("number", await SveveEndpoint.Endpoint(null!, " "));
        AssertBadRequest("number", await SveveEndpoint.Endpoint(null!, "\n"));
        AssertBadRequest("number", await SveveEndpoint.Endpoint(null!, "\r"));
        AssertBadRequest("number", await SveveEndpoint.Endpoint(null!, "\t"));
    }

    [Fact]
    public async Task DetectMissingMessage()
    {
        AssertBadRequest("msg", await SveveEndpoint.Endpoint(null!, Number, msg:null));
        AssertBadRequest("msg", await SveveEndpoint.Endpoint(null!, Number, msg:""));
        AssertBadRequest("msg", await SveveEndpoint.Endpoint(null!, Number, msg:" "));
        AssertBadRequest("msg", await SveveEndpoint.Endpoint(null!, Number, msg:"\t"));
    }

    [Fact]
    public async Task DetectAmbiguousNotification()
    {
        var services = new ServiceCollection().AddSveveConsumer<TestConsumer>().BuildServiceProvider();
        AssertBadRequest("ambiguous", await SveveEndpoint.Endpoint(services, Number, msg: Msg));
        AssertBadRequest("ambiguous", await SveveEndpoint.Endpoint(services, Number, msg: Msg, shortnumber:""));
        AssertBadRequest("ambiguous", await SveveEndpoint.Endpoint(services, Number, msg: Msg, shortnumber:" "));
        AssertBadRequest("ambiguous", await SveveEndpoint.Endpoint(services, Number, msg: Msg, shortnumber:"\t"));
        AssertBadRequest("ambiguous", await SveveEndpoint.Endpoint(services, Number, msg: Msg, prefix: ""));
        AssertBadRequest("ambiguous", await SveveEndpoint.Endpoint(services, Number, msg: Msg, prefix: " "));
        AssertBadRequest("ambiguous", await SveveEndpoint.Endpoint(services, Number, msg: Msg, prefix: "\t"));
    }

    [Fact]
    public async Task DetectMissingDeliveryId()
    {
        AssertBadRequest("id", await SveveEndpoint.Endpoint(null!, Number, status: true));
        AssertBadRequest("id", await SveveEndpoint.Endpoint(null!, Number, status: false));
    }

    [Fact]
    public async Task DetectDeliveryReport()
    {
        var services = new ServiceCollection().AddSveveConsumer<TestConsumer>().BuildServiceProvider();
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: true, id: 1, refParam: null));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: true, id: 1, refParam: ""));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: true, id: 1, refParam: " "));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: true, id: 1, refParam: "\t"));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: true, id: 1, refParam: "Hello"));

        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, refParam: null));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, refParam: ""));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, refParam: " "));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, refParam: "\t"));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, refParam: "Hello"));

        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, errorCode: "ABC123", errorDesc: null));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, errorCode: null, errorDesc: "ABC123"));
        AssertOk("Delivery report accepted", await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, errorCode: "ABC123", errorDesc: "ABC123"));
    }

    [Fact]
    public async Task DetectMissingDeliveryConsumer()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        AssertInternalServerError(await SveveEndpoint.Endpoint(services, Number, status: true, id: 1));
        AssertInternalServerError(await SveveEndpoint.Endpoint(services, Number, status: false, id: 1));
    }

    [Fact]
    public async Task DetectMissingSmsConsumer ()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        AssertInternalServerError(await SveveEndpoint.Endpoint(services, Number, msg: Msg, shortnumber:"1234"));
        AssertInternalServerError(await SveveEndpoint.Endpoint(services, Number, msg: Msg, prefix:"1234"));
        AssertInternalServerError(await SveveEndpoint.Endpoint(services, Number, msg: Msg, id:1));
    }

    [Fact]
    public async Task DetectSmsToDedicatedPhoneNumber()
    {
        var services = new ServiceCollection().AddSveveConsumer<TestConsumer>().BuildServiceProvider();
        AssertOk("dedicated phone number", await SveveEndpoint.Endpoint(services, Number, msg:Msg, shortnumber:"1234"));
    }

    [Fact]
    public async Task DetectSmsToCodeWord()
    {
        var services = new ServiceCollection().AddSveveConsumer<TestConsumer>().BuildServiceProvider();
        AssertOk("code word", await SveveEndpoint.Endpoint(services, Number, msg: Msg, prefix: "AWESOME"));
    }

    [Fact]
    public async Task DetectSmsToReply()
    {
        var services = new ServiceCollection().AddSveveConsumer<TestConsumer>().BuildServiceProvider();
        AssertOk("Reply", await SveveEndpoint.Endpoint(services, Number, msg: Msg, id:1));
    }

    [Fact]
    public async Task ConsumesDeliveryOk()
    {
        var consumer = new TestConsumer();
        var services = new ServiceCollection().AddSingleton<ISveveDeliveryConsumer>(consumer).BuildServiceProvider();
        var result = await SveveEndpoint.Endpoint(services, Number, status: true, id: 1, refParam: "Reference");
        Assert.IsType<Ok<string>>(result);
        
        Assert.NotNull(consumer.Delivered);
        Assert.Null(consumer.Failed);
        Assert.Null(consumer.Error);
        Assert.Equal(Number, consumer.Delivered.ReceiverPhoneNumber);
        Assert.Equal(1, consumer.Delivered.MessageId);
        Assert.Equal("Reference", consumer.Delivered.Reference);
    }


    [Fact]
    public async Task ConsumesDeliveryFailed()
    {
        var consumer = new TestConsumer();
        var services = new ServiceCollection().AddSingleton<ISveveDeliveryConsumer>(consumer).BuildServiceProvider();
        var result = await SveveEndpoint.Endpoint(services, Number, status: false, id: 1, refParam: "Reference", errorCode:"Error", errorDesc:"Description");
        Assert.IsType<Ok<string>>(result);

        Assert.Null(consumer.Delivered);
        Assert.NotNull(consumer.Failed);
        Assert.NotNull(consumer.Error);
        Assert.Equal(Number, consumer.Failed.ReceiverPhoneNumber);
        Assert.Equal(1, consumer.Failed.MessageId);
        Assert.Equal("Reference", consumer.Failed.Reference);
        Assert.Equal("Error", consumer.Error.Code);
        Assert.Equal("Description", consumer.Error.Description);
    }


    [Fact]
    public async Task ConsumesSmsReply()
    {
        var consumer = new TestConsumer();
        var services = new ServiceCollection().AddSingleton<ISveveSmsConsumer>(consumer).BuildServiceProvider();
        var result = await SveveEndpoint.Endpoint(services, Number, id: 1, msg:"Reply");
        Assert.IsType<Ok<string>>(result);

        Assert.Null(consumer.ToCode);
        Assert.Null(consumer.ToDedicatedPhoneNumber);
        Assert.NotNull(consumer.Reply);
        Assert.Equal(Number, consumer.Reply.SenderPhoneNumber);
        Assert.Equal(1, consumer.Reply.MessageId);
        Assert.Equal("Reply", consumer.Reply.Message);
    }


    [Fact]
    public async Task ConsumesSmsCodeWord()
    {
        var consumer = new TestConsumer();
        var services = new ServiceCollection().AddSingleton<ISveveSmsConsumer>(consumer).BuildServiceProvider();
        var result = await SveveEndpoint.Endpoint(services, Number, prefix: "my-code", msg: "ToCode");
        Assert.IsType<Ok<string>>(result);

        Assert.Null(consumer.Reply);
        Assert.Null(consumer.ToDedicatedPhoneNumber);
        Assert.NotNull(consumer.ToCode);
        Assert.Equal(Number, consumer.ToCode.SenderPhoneNumber);
        Assert.Equal("my-code", consumer.ToCode.Code);
        Assert.Equal("ToCode", consumer.ToCode.Message);
    }


    [Fact]
    public async Task ConsumesSmsDedicatedPhoneNumber()
    {
        var consumer = new TestConsumer();
        var services = new ServiceCollection().AddSingleton<ISveveSmsConsumer>(consumer).BuildServiceProvider();
        var result = await SveveEndpoint.Endpoint(services, Number, shortnumber:"1234", msg: "ToNumber");
        Assert.IsType<Ok<string>>(result);

        Assert.Null(consumer.Reply);
        Assert.Null(consumer.ToCode);
        Assert.NotNull(consumer.ToDedicatedPhoneNumber);
        Assert.Equal(Number, consumer.ToDedicatedPhoneNumber.SenderPhoneNumber);
        Assert.Equal("1234", consumer.ToDedicatedPhoneNumber.DedicatedPhoneNumber);
        Assert.Equal("ToNumber", consumer.ToDedicatedPhoneNumber.Message);
    }

    [Fact]
    public void AddConsumerExtensions()
    {
        new ServiceCollection()
            .AddSveveDeliveryConsumer<TestConsumer>()
            .BuildServiceProvider()
            .GetRequiredService<ISveveDeliveryConsumer>();

        new ServiceCollection()
            .AddSveveSmsConsumer<TestConsumer>()
            .BuildServiceProvider()
            .GetRequiredService<ISveveSmsConsumer>();

        var provider = new ServiceCollection()
            .AddSveveConsumer<TestConsumer>()
            .BuildServiceProvider();

        provider.GetRequiredService<ISveveDeliveryConsumer>();
        provider.GetRequiredService<ISveveSmsConsumer>();
    }

    [Theory]
    [InlineData("", HttpStatusCode.BadRequest)]
    [InlineData("number=11111111&status=true&id=1", HttpStatusCode.OK)]
    [InlineData("number=11111111&status=false&id=1&errorCode=e1&errorDesc=Unknown%20error", HttpStatusCode.OK)]
    [InlineData("number=11111111&msg=Hello%20world&prefix=abc123", HttpStatusCode.OK)]
    [InlineData("number=11111111&msg=Hello%20world&shortnumber=1111", HttpStatusCode.OK)]
    [InlineData("number=11111111&msg=Hello%20world&id=1", HttpStatusCode.OK)]
    public async Task MapSveveConsumerEndpoint(string queryParameterString, HttpStatusCode status)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSveveConsumer<NullConsumer>();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        // Ensure that this actually works.
        app.MapSveveConsumerEndpoint("api/sveve");

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("api/sveve?" + queryParameterString);
        Assert.Equal(status, response.StatusCode);
    }

    class NullConsumer : ISveveDeliveryConsumer, ISveveSmsConsumer
    {
        public Task SmsDelivered(OutgoingSms sms, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SmsFailed(OutgoingSms sms, SmsDeliveryError error, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SmsReceived(IncomingSmsReply sms, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SmsReceived(IncomingSmsToCode sms, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SmsReceived(IncomingSmsToDedicatedPhoneNumber sms, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static void AssertBadRequest(string partialMessage, IResult actualResult)
    {
        var badRequest = Assert.IsType<BadRequest<string>>(actualResult);
        Assert.Contains(partialMessage, badRequest.Value);
    }

    private static void AssertOk(string partialMessage, IResult actualResult)
    {
        var badRequest = Assert.IsType<Ok<string>>(actualResult);
        Assert.Contains(partialMessage, badRequest.Value);
    }

    private static void AssertInternalServerError(IResult actualResult)
    {
        var statusCode = Assert.IsType<StatusCodeHttpResult>(actualResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode.StatusCode);
    }
}

file sealed class TestConsumer : ISveveDeliveryConsumer, ISveveSmsConsumer
{
    public OutgoingSms? Delivered { get; private set; }
    public OutgoingSms? Failed { get; private set; }
    public SmsDeliveryError? Error { get; private set; }
    public IncomingSmsReply? Reply { get; private set; }
    public IncomingSmsToCode? ToCode { get; private set; }
    public IncomingSmsToDedicatedPhoneNumber? ToDedicatedPhoneNumber { get; private set; }

    public Task SmsDelivered(OutgoingSms deliveredSms, CancellationToken cancellationToken)
    {
        Delivered = deliveredSms;
        return Task.CompletedTask;
    }

    public Task SmsFailed(OutgoingSms failedSms, SmsDeliveryError error, CancellationToken cancellationToken)
    {
        Failed = failedSms;
        Error = error;
        return Task.CompletedTask;
    }

    public Task SmsReceived(IncomingSmsReply sms, CancellationToken cancellationToken)
    {
        Reply = sms;
        return Task.CompletedTask;
    }

    public Task SmsReceived(IncomingSmsToCode sms, CancellationToken cancellationToken)
    {
        ToCode = sms;
        return Task.CompletedTask;
    }

    public Task SmsReceived(IncomingSmsToDedicatedPhoneNumber sms, CancellationToken cancellationToken)
    {
        ToDedicatedPhoneNumber = sms;
        return Task.CompletedTask;
    }
}