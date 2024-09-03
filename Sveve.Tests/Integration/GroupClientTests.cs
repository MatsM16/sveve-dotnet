namespace Sveve.Tests.Integration;

public class GroupClientTests : IAsyncDisposable
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
    public async Task GroupManagement()
    {
        await _client.Groups.CreateAsync(GroupA);
        var groups = await _client.Groups.ListAsync();
        Assert.Contains(GroupA, groups);

        await _client.Groups.AddRecipientAsync(GroupA, PersonA.Name, PersonA.PhoneNumber);
        await _client.Groups.AddRecipientAsync(GroupA, PersonB.Name, PersonB.PhoneNumber);
        var recipients = await _client.Groups.ListRecipientsAsync(GroupA);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);

        await _client.Groups.MoveRecipientsAsync(GroupA, GroupB);
        recipients = await _client.Groups.ListRecipientsAsync(GroupB);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);

        groups = await _client.Groups.ListAsync();
        Assert.Contains(GroupA, groups);
        Assert.Contains(GroupB, groups);

        await _client.Groups.MoveRecipientAsync(GroupB, GroupC, PersonA.PhoneNumber);

        recipients = await _client.Groups.ListRecipientsAsync(GroupB);
        Assert.DoesNotContain(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);

        recipients = await _client.Groups.ListRecipientsAsync(GroupC);
        Assert.Contains(recipients, x => x.PhoneNumber == PersonA.PhoneNumber);
        Assert.DoesNotContain(recipients, x => x.PhoneNumber == PersonB.PhoneNumber);

        await _client.Groups.RemoveRecipientAsync(GroupC, PersonA.PhoneNumber);
        Assert.Empty(await _client.Groups.ListRecipientsAsync(GroupC));

        await _client.Groups.DeleteAsync(GroupA);
        await _client.Groups.DeleteAsync(GroupB);
        await _client.Groups.DeleteAsync(GroupC);
        groups = await _client.Groups.ListAsync();
        Assert.True(groups.Count == 1);
        Assert.True(groups[0] == "Min gruppe");
    }

    public async ValueTask DisposeAsync()
    {
        await _client.Groups.DeleteAsync(GroupA);
        await _client.Groups.DeleteAsync(GroupB);
        await _client.Groups.DeleteAsync(GroupC);
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    private record TestPerson(string Name, string PhoneNumber);
}