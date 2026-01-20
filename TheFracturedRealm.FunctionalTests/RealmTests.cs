using Shouldly;

namespace TheFracturedRealm.FunctionalTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public sealed class RealmTests : IClassFixture<RealmHostFixture>
{
    private readonly RealmHostFixture _fixture;
    public RealmTests(RealmHostFixture fixture) => _fixture = fixture;
    [Fact, TestPriority(0)]
    public async Task ConnectReceivesWelcomeAndHandlePrompt()
    {
        // Arrange
        await using var c = new RealmClient();

        // Act
        var welcome = await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Welcome to The Fractured Realm!", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        var prompt = await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Your handle? Type:", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));

        // Assert
        welcome.ShouldContain("Welcome to The Fractured Realm!", Case.Insensitive);
        prompt.ShouldContain("Your handle? Type:", Case.Insensitive);
    }
}
