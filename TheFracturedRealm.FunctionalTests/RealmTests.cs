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
        var lines = await c.ExpectAsync(TimeSpan.FromSeconds(2), "Welcome to The Fractured Realm!", "Your handle? Type:");

        // Assert
        Sanitizer.StripAnsi(lines[0]).ShouldContain("Welcome to The Fractured Realm!", Case.Insensitive);
        Sanitizer.StripAnsi(lines[1]).ShouldContain("Your handle? Type:", Case.Insensitive);
    }

    [Fact, TestPriority(1)]
    public async Task CannotSayBeforeNaming()
    {
        // Arrange
        await using var c = new RealmClient();
        await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Welcome to The Fractured Realm!", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Your handle? Type: name <yourname>", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));

        // Act
        await c.SendLineAsync("say Hello everyone!", TestContext.Current.CancellationToken);
        var response = await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Set your handle first:", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        var plain = Sanitizer.StripAnsi(response);

        // Assert
        plain.ShouldContain("Set your handle first: name <yourname>", Case.Insensitive);
    }

    [Fact, TestPriority(2)]
    public async Task CanSetPlayerName()
    {
        // Arrange
        await using var c = new RealmClient();
        await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Welcome to The Fractured Realm!", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Your handle? Type: name <yourname>", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));

        // Act
        await c.SendLineAsync("name Alice", TestContext.Current.CancellationToken);
        var response = await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Welcome, Alice!", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        var plain = Sanitizer.StripAnsi(response);

        // Assert
        plain.ShouldContain("Welcome, Alice! Type help to get started.", Case.Insensitive);
    }

    [Fact, TestPriority(3)]
    public async Task CanSayAfterNaming()
    {
        // Arrange
        await using var c = new RealmClient();
        await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Welcome to The Fractured Realm!", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Your handle? Type: name <yourname>", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        await c.SendLineAsync("name Bob", TestContext.Current.CancellationToken);
        await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("Welcome, Bob!", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));

        // Act
        await c.SendLineAsync("say Hello everyone!", TestContext.Current.CancellationToken);
        var response = await c.WaitForLineAsync(line => Sanitizer.StripAnsi(line).Contains("You say: Hello everyone!", StringComparison.OrdinalIgnoreCase), timeout: TimeSpan.FromSeconds(2));
        var plain = Sanitizer.StripAnsi(response);

        // Assert
        plain.ShouldContain("You say: Hello everyone!", Case.Insensitive);
    }
}
