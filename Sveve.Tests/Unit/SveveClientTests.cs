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

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.SendAsync(new Sms("a", "b")));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.RemainingSmsUnitsAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.PurchaseSmsUnitsAsync(SmsUnitOrder.Bulk500));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GroupsAsync());

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").CreateAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").DeleteAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").ExistsAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").AddMemberAsync("1234"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").RemoveMemberAsync("1234"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").MoveToAsync("g2"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").MoveToAsync("g2", "1234"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Group("g").HasMemberAsync("1234"));
    }

    [Fact]
    public async Task PurchaseRequiresUnitOrder()
    {
        using var client = new SveveClient("invalid", "invalid");
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.PurchaseSmsUnitsAsync(null!));
    }

    [Fact]
    public async Task CannotSendNullSms()
    {
        using var client = new SveveClient("invalid", "invalid");
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync((Sms)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync((IEnumerable<Sms>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync([null!]));
    }

    [Fact]
    public async Task AllMessagesMustAgreeOnTest()
    {
        var a = new Sms("a", "a") { Test = false };
        var at = new Sms("a", "at") { Test = true };
        var b = new Sms("b", "b") { Test = false };
        var bt = new Sms("b", "bt") { Test = true };

        using var client = new SveveClient("invalid", "invalid");
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.SendAsync([a, b]));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.SendAsync([at, bt]));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SendAsync([a, bt]));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SendAsync([at, b]));
    }
}
