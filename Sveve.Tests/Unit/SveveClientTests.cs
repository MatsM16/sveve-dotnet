using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Sveve.Tests.Unit;

public class SveveClientTests
{
    [Fact]
    public void UsernameAndPasswordIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new SveveClient(null!, null!));
        Assert.Throws<ArgumentNullException>(() => new SveveClient(null!));
        Assert.Throws<ArgumentNullException>(() => new SveveClient(new SveveClientOptions()));
    }

    [Fact]
    public async Task ThrowsWhenDisposed()
    {
        var client = new SveveClient("invalid", "invalid");
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.SendAsync(new Sms("a", "b"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.RemainingSmsUnitsAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.PurchaseSmsUnitsAsync(SmsUnitOrder.Bulk500, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GroupsAsync(TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").CreateAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").DeleteAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").ExistsAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").AddMemberAsync("1234", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").RemoveMemberAsync("1234", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").MoveToAsync("g2", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").MoveToAsync("g2", "1234", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").HasMemberAsync("1234", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PurchaseRequiresUnitOrder()
    {
        using var client = new SveveClient("invalid", "invalid");
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.PurchaseSmsUnitsAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CannotSendNullSms()
    {
        using var client = new SveveClient("invalid", "invalid");
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync((Sms)null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync((IEnumerable<Sms>)null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync([null!], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AllMessagesMustAgreeOnTest()
    {
        var a = new Sms("a", "a") { Test = false };
        var at = new Sms("a", "at") { Test = true };
        var b = new Sms("b", "b") { Test = false };
        var bt = new Sms("b", "bt") { Test = true };

        using var client = new SveveClient("invalid", "invalid");
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.SendAsync([a, b], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.SendAsync([at, bt], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SendAsync([a, bt], TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SendAsync([at, b], TestContext.Current.CancellationToken));
    }
}
