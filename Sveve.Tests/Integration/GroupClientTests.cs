using System.Security.Authentication;

namespace Sveve.Tests.Integration;

public class GroupClientTests : IAsyncLifetime
{
    private string GroupA = "test-group-" + Guid.NewGuid();
    private string GroupB = "test-group-" + Guid.NewGuid();
    private string GroupC = "test-group-" + Guid.NewGuid();
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
        await _client.Group.CreateAsync(GroupA);
        var groups = await _client.Group.ListAsync();
        Assert.Contains(GroupA, groups);
    }

    [Fact]
    public async Task AddRecipient()
    {
        await _client.Group.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        await _client.Group.AddRecipientAsync(GroupA, PersonB.Name, PersonB.PhoneNumber);
        var recipients = await _client.Group.ListRecipientsAsync(GroupA);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveGroupRecipients()
    {
        await _client.Group.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        await _client.Group.AddRecipientAsync(GroupA, PersonB.Name, PersonB.PhoneNumber);
        
        await _client.Group.MoveRecipientsAsync(GroupA, GroupB);
        
        var recipientsInB = await _client.Group.ListRecipientsAsync(GroupB);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonB.PhoneNumber);
        
        var recipientsInA = await _client.Group.ListRecipientsAsync(GroupA);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveSingleReceipient()
    {
        await _client.Group.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        
        await _client.Group.MoveRecipientsAsync(GroupA, GroupB);

        var recipientsInB = await _client.Group.ListRecipientsAsync(GroupB);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);

        var recipientsInA = await _client.Group.ListRecipientsAsync(GroupA);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task RemoveRecipients()
    {
        var beforeAdd = await _client.Group.ListRecipientsAsync(GroupA);
        Assert.Empty(beforeAdd);

        await _client.Group.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        var afterAdd = await _client.Group.ListRecipientsAsync(GroupA);
        Assert.Contains(afterAdd, x => x.PhoneNumber == PersonA.PhoneNumber);

        await _client.Group.RemoveRecipientAsync(GroupA, PersonA.PhoneNumber);
        var afterRemove = await _client.Group.ListRecipientsAsync(GroupA);
        Assert.DoesNotContain(afterRemove, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task DeleteGroup()
    {
        var beforeAdd = await _client.Group.ListAsync();
        Assert.DoesNotContain(GroupA, beforeAdd);

        await _client.Group.CreateAsync(GroupA);
        var afterAdd = await _client.Group.ListAsync();
        Assert.Contains(GroupA, afterAdd);

        await _client.Group.DeleteAsync(GroupA);
        var afterDelete = await _client.Group.ListAsync();
        Assert.DoesNotContain(GroupA, afterDelete);
    }

    [Fact]
    public async Task Exists()
    {
        Assert.False(await _client.Group.ExistsAsync(GroupA));
        await _client.Group.CreateAsync(GroupA);
        Assert.True(await _client.Group.ExistsAsync(GroupA));
        await _client.Group.DeleteAsync(GroupA);
        Assert.False(await _client.Group.ExistsAsync(GroupA));
    }

    [Fact]
    public async Task HasReceipient()
    {
        Assert.False(await _client.Group.HasRecipientAsync(GroupA, PersonA.PhoneNumber));
        await _client.Group.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        Assert.True(await _client.Group.HasRecipientAsync(GroupA, PersonA.PhoneNumber));
        await _client.Group.RemoveRecipientAsync(GroupA, PersonA.PhoneNumber);
        Assert.False(await _client.Group.HasRecipientAsync(GroupA, PersonA.PhoneNumber));
    }

    [Fact]
    public async Task ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid"
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.ListAsync());
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.CreateAsync("group"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.DeleteAsync("group"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.ExistsAsync("group"));

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.ListRecipientsAsync("group"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.AddRecipientAsync("group", "name", "number"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.RemoveRecipientAsync("group", "number"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.HasRecipientAsync("group", "number"));

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.MoveRecipientsAsync("from", "to"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group.MoveRecipientAsync("from", "to", "number"));
    }

    public async Task DisposeAsync()
    {
        await _client.Group.DeleteAsync(GroupA);
        await _client.Group.DeleteAsync(GroupB);
        await _client.Group.DeleteAsync(GroupC);
        _client.Dispose();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    private record TestPerson(string Name, string PhoneNumber);
}