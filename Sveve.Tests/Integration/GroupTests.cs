using System.Security.Authentication;

namespace Sveve.Tests.Integration;

public class GroupTests : IAsyncLifetime
{
    private static readonly TestPerson PersonA = new("Line Danser", "99999999");
    private static readonly TestPerson PersonB = new("Roland Gundersen", "44444444");

    private readonly string GroupA = "test-group-" + Guid.NewGuid();
    private readonly string GroupB = "test-group-" + Guid.NewGuid();
    private readonly SveveClient _client = new(new()
    {
        Username = TestEnvironment.Configuration["SVEVE:USERNAME"]!,
        Password = TestEnvironment.Configuration["SVEVE:PASSWORD"]!,
        Test = true
    });

    [Fact]
    public async Task CreateGroup()
    {
        var group = _client.Group(GroupA);
        await group.CreateAsync();
        var groups = await _client.GroupsAsync();
        Assert.Contains(GroupA, groups);
    }

    [Fact]
    public async Task AddRecipient()
    {
        var group = _client.Group(GroupA);
        await group.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name);
        await group.AddMemberAsync(PersonB.PhoneNumber, PersonB.Name);
        var recipients = await group.MembersAsync();
        Assert.Contains(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveGroupRecipients()
    {
        var groupA = _client.Group(GroupA);
        await groupA.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name);
        await groupA.AddMemberAsync(PersonB.PhoneNumber, PersonB.Name);
        
        await groupA.MoveToAsync(GroupB);

        var groupB = _client.Group(GroupB);
        var recipientsInB = await groupB.MembersAsync();
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonB.PhoneNumber);
        
        var recipientsInA = await groupA.MembersAsync();
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveSingleRecipient()
    {
        var groupA = _client.Group(GroupA);
        await groupA.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name);
        
        await groupA.MoveToAsync(GroupB, PersonA.PhoneNumber);

        var groupB = _client.Group(GroupB);
        var recipientsInB = await groupB.MembersAsync();
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);

        var recipientsInA = await groupA.MembersAsync();
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task RemoveRecipients()
    {
        var group = _client.Group(GroupA);
        var beforeAdd = await group.MembersAsync();
        Assert.Empty(beforeAdd);

        await group.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name);
        var afterAdd = await group.MembersAsync();
        Assert.Contains(afterAdd, x => x.PhoneNumber == PersonA.PhoneNumber);

        await group.RemoveMemberAsync(PersonA.PhoneNumber);
        var afterRemove = await group.MembersAsync();
        Assert.DoesNotContain(afterRemove, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task DeleteGroup()
    {
        var beforeAdd = await _client.GroupsAsync();
        Assert.DoesNotContain(GroupA, beforeAdd);

        var group = _client.Group(GroupA);
        await group.CreateAsync();
        var afterAdd = await _client.GroupsAsync();
        Assert.Contains(GroupA, afterAdd);

        await group.DeleteAsync();
        var afterDelete = await _client.GroupsAsync();
        Assert.DoesNotContain(GroupA, afterDelete);
    }

    [Fact]
    public async Task Exists()
    {
        var group = _client.Group(GroupA);
        Assert.False(await group.ExistsAsync());

        await group.CreateAsync();
        Assert.True(await group.ExistsAsync());

        await group.DeleteAsync();
        Assert.False(await group.ExistsAsync());
    }

    [Fact]
    public async Task HasRecipient()
    {
        var group = _client.Group(GroupA);
        Assert.False(await group.HasMemberAsync(PersonA.PhoneNumber));

        await group.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name);
        Assert.True(await group.HasMemberAsync(PersonA.PhoneNumber));

        await group.RemoveMemberAsync(PersonA.PhoneNumber);
        Assert.False(await group.HasMemberAsync(PersonA.PhoneNumber));
    }

    [Fact]
    public async Task ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid"
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.GroupsAsync());
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").CreateAsync());
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").DeleteAsync());
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").ExistsAsync());
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").MembersAsync());
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").AddMemberAsync("number", "name"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").RemoveMemberAsync("number"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").HasMemberAsync("number"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").MoveToAsync("other_group"));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").MoveToAsync("other_group", "number"));
    }

    public async Task DisposeAsync()
    {
        await _client.Group(GroupA).DeleteAsync();
        await _client.Group(GroupB).DeleteAsync();
        _client.Dispose();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    private record TestPerson(string Name, string PhoneNumber);
}