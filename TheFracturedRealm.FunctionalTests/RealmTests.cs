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
        var lines = await c.ExpectAsync(RealmClient.DefaultTimeout, "Welcome to The Fractured Realm!", "Your handle? Type:");

        // Assert
        lines[0].ShouldContainWithoutAnsi("Welcome to The Fractured Realm!");
        lines[1].ShouldContainWithoutAnsi("Your handle? Type:");
    }

    [Fact, TestPriority(1)]
    public async Task CannotSayBeforeNaming()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAtPromptAsync();

        // Act
        var response = await c.SendAndWaitAsync("say Hello everyone!", "Set your handle first:", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("Set your handle first: name <yourname>");
    }

    [Fact, TestPriority(2)]
    public async Task CanSetPlayerName()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAtPromptAsync();

        // Act
        var response = await c.SendAndWaitAsync("name Alice", "Welcome, Alice!", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("Welcome, Alice! Type help to get started.");
    }

    [Fact, TestPriority(3)]
    public async Task CanSayAfterNaming()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("Bob", ct: TestContext.Current.CancellationToken);

        // Act
        var response = await c.SendAndWaitAsync("say Hello everyone!", "You say: Hello everyone!", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("You say: Hello everyone!");
    }
}
