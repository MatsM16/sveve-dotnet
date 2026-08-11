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
        await group.CreateAsync(TestContext.Current.CancellationToken);
        var groups = await _client.GroupsAsync(TestContext.Current.CancellationToken);
        Assert.Contains(GroupA, groups);
    }

    [Fact]
    public async Task AddRecipient()
    {
        var group = _client.Group(GroupA);
        await group.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name, TestContext.Current.CancellationToken);
        await group.AddMemberAsync(PersonB.PhoneNumber, PersonB.Name, TestContext.Current.CancellationToken);
        var recipients = await group.MembersAsync(TestContext.Current.CancellationToken);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveGroupRecipients()
    {
        var groupA = _client.Group(GroupA);
        await groupA.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name, TestContext.Current.CancellationToken);
        await groupA.AddMemberAsync(PersonB.PhoneNumber, PersonB.Name, TestContext.Current.CancellationToken);
        
        await groupA.MoveToAsync(GroupB, TestContext.Current.CancellationToken);

        var groupB = _client.Group(GroupB);
        var recipientsInB = await groupB.MembersAsync(TestContext.Current.CancellationToken);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonB.PhoneNumber);
        
        var recipientsInA = await groupA.MembersAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonB.PhoneNumber);
    }

    [Fact]
    public async Task MoveSingleRecipient()
    {
        var groupA = _client.Group(GroupA);
        await groupA.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name, TestContext.Current.CancellationToken);
        
        await groupA.MoveToAsync(GroupB, PersonA.PhoneNumber, TestContext.Current.CancellationToken);

        var groupB = _client.Group(GroupB);
        var recipientsInB = await groupB.MembersAsync(TestContext.Current.CancellationToken);
        Assert.Contains(recipientsInB, x => x.PhoneNumber == PersonA.PhoneNumber);

        var recipientsInA = await groupA.MembersAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(recipientsInA, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task RemoveRecipients()
    {
        var group = _client.Group(GroupA);
        var beforeAdd = await group.MembersAsync(TestContext.Current.CancellationToken);
        Assert.Empty(beforeAdd);

        await group.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name, TestContext.Current.CancellationToken);
        var afterAdd = await group.MembersAsync(TestContext.Current.CancellationToken);
        Assert.Contains(afterAdd, x => x.PhoneNumber == PersonA.PhoneNumber);

        await group.RemoveMemberAsync(PersonA.PhoneNumber, TestContext.Current.CancellationToken);
        var afterRemove = await group.MembersAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(afterRemove, x => x.PhoneNumber == PersonA.PhoneNumber);
    }

    [Fact]
    public async Task DeleteGroup()
    {
        var beforeAdd = await _client.GroupsAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(GroupA, beforeAdd);

        var group = _client.Group(GroupA);
        await group.CreateAsync(TestContext.Current.CancellationToken);
        var afterAdd = await _client.GroupsAsync(TestContext.Current.CancellationToken);
        Assert.Contains(GroupA, afterAdd);

        await group.DeleteAsync(TestContext.Current.CancellationToken);
        var afterDelete = await _client.GroupsAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(GroupA, afterDelete);
    }

    [Fact]
    public async Task Exists()
    {
        var group = _client.Group(GroupA);
        Assert.False(await group.ExistsAsync(TestContext.Current.CancellationToken));

        await group.CreateAsync(TestContext.Current.CancellationToken);
        Assert.True(await group.ExistsAsync(TestContext.Current.CancellationToken));

        await group.DeleteAsync(TestContext.Current.CancellationToken);
        Assert.False(await group.ExistsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HasRecipient()
    {
        var group = _client.Group(GroupA);
        Assert.False(await group.HasMemberAsync(PersonA.PhoneNumber, TestContext.Current.CancellationToken));

        await group.AddMemberAsync(PersonA.PhoneNumber, PersonA.Name, TestContext.Current.CancellationToken);
        Assert.True(await group.HasMemberAsync(PersonA.PhoneNumber, TestContext.Current.CancellationToken));

        await group.RemoveMemberAsync(PersonA.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.False(await group.HasMemberAsync(PersonA.PhoneNumber, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ThrowsInvalidCredentialException()
    {
        var client = new SveveClient(new()
        {
            Username = "invalid",
            Password = "invalid"
        });

        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.GroupsAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").CreateAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").DeleteAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").ExistsAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").MembersAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").AddMemberAsync("number", "name", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").RemoveMemberAsync("number", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").HasMemberAsync("number", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").MoveToAsync("other_group", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidCredentialException>(() => client.Group("group").MoveToAsync("other_group", "number", TestContext.Current.CancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        await _client.Group(GroupA).DeleteAsync(TestContext.Current.CancellationToken);
        await _client.Group(GroupB).DeleteAsync(TestContext.Current.CancellationToken);
        _client.Dispose();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    private record TestPerson(string Name, string PhoneNumber);
}