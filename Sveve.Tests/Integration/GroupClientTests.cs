using System.Security.Authentication;

namespace Sveve.Tests.Integration;

public class GroupClientTests : IAsyncLifetime
{
    private const string GroupA = "test-group-a";
    private const string GroupB = "test-group-b";
    private const string GroupC = "test-group-c";
    private static readonly TestPerson PersonA = new("Line Danser", "99999999");
    private static readonly TestPerson PersonB = new("Roland Gundersen", "44444444");

    private readonly SveveClient _client = new(new()
    {
        Username = TestEnvironment.Configuration["SVEVE:USERNAME"]!,
        Password = TestEnvironment.Configuration["SVEVE:PASSWORD"]!,
        IsTest = true
    });

    [Fact]
    public async Task CreateGroup()
    {
        await _client.Groups.CreateAsync(GroupA);
        var groups = await _client.Groups.ListAsync();
        Assert.Contains(GroupA, groups);
    }

    [Fact]
    public async Task AddRecipient()
    {
        await _client.Groups.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        await _client.Groups.AddRecipientAsync(GroupA, PersonB.Name, PersonB.PhoneNumber);
        var recipients = await _client.Groups.ListRecipientsAsync(GroupA);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveGroupRecipients()
    {
        await _client.Groups.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        await _client.Groups.AddRecipientAsync(GroupA, PersonB.Name, PersonB.PhoneNumber);
        
        await _client.Groups.MoveRecipientsAsync(GroupA, GroupB);
        
        var recipientsInB = await _client.Groups.ListRecipientsAsync(GroupB);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonB.PhoneNumber);
        
        var recipientsInA = await _client.Groups.ListRecipientsAsync(GroupA);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveSingleReceipient()
    {
        await _client.Groups.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        
        await _client.Groups.MoveRecipientsAsync(GroupA, GroupB);

        var recipientsInB = await _client.Groups.ListRecipientsAsync(GroupB);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);

        var recipientsInA = await _client.Groups.ListRecipientsAsync(GroupA);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task RemoveRecipients()
    {
        var beforeAdd = await _client.Groups.ListRecipientsAsync(GroupA);
        Assert.Empty(beforeAdd);

        await _client.Groups.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        var afterAdd = await _client.Groups.ListRecipientsAsync(GroupA);
        Assert.Contains(afterAdd, x => x.PhoneNumber == PersonA.PhoneNumber);

        await _client.Groups.RemoveRecipientAsync(GroupA, PersonA.PhoneNumber);
        var afterRemove = await _client.Groups.ListRecipientsAsync(GroupA);
        Assert.DoesNotContain(afterRemove, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task DeleteGroup()
    {
        var beforeAdd = await _client.Groups.ListAsync();
        Assert.DoesNotContain(GroupA, beforeAdd);

        await _client.Groups.CreateAsync(GroupA);
        var afterAdd = await _client.Groups.ListAsync();
        Assert.Contains(GroupA, afterAdd);

        await _client.Groups.DeleteAsync(GroupA);
        var afterDelete = await _client.Groups.ListAsync();
        Assert.DoesNotContain(GroupA, afterDelete);
    }

    [Fact]
    public async Task ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid"
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.ListAsync());
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.CreateAsync("group"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.DeleteAsync("group"));

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.ListRecipientsAsync("group"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.AddRecipientAsync("group", "name", "number"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.RemoveRecipientAsync("group", "number"));

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.MoveRecipientsAsync("from", "to"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Groups.MoveRecipientAsync("from", "to", "number"));
    }

    public async Task DisposeAsync()
    {
        await _client.Groups.DeleteAsync(GroupA);
        await _client.Groups.DeleteAsync(GroupB);
        await _client.Groups.DeleteAsync(GroupC);
        _client.Dispose();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    private record TestPerson(string Name, string PhoneNumber);
}