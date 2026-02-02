using Shouldly;

namespace TheFracturedRealm.FunctionalTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public sealed class RealmTests : IClassFixture<RealmHostFixture>
{
    private readonly RealmHostFixture _fixture;
    public RealmTests(RealmHostFixture fixture) => _fixture = fixture;

    [Fact, TestPriority(0)]
    public async Task OnConnectShowsWelcomeAndAsksForHandle()
    {
        // Arrange
        await using var c = new RealmClient();

        // Act
        var lines = await c.ExpectAsync(RealmClient.DefaultTimeout, "Welcome to The Fractured Realm!", "Your handle? Type: name <yourname>");

        // Assert
        lines[0].ShouldContainWithoutAnsi("Welcome to The Fractured Realm!");
        lines[1].ShouldContainWithoutAnsi("Your handle? Type: name <yourname>");
    }

    [Fact, TestPriority(1)]
    public async Task CannotSayBeforeNaming()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAtPromptAsync();

        // Act
        var response = await c.SendAndWaitAsync("say Hello everyone!", "Set your handle first: name <yourname>", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("Set your handle first: name <yourname>");
    }

    [Fact, TestPriority(2)]
    public async Task NameCommandSetsHandleAndShowsGettingStartedHint()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAtPromptAsync();

        // Act
        var response = await c.SendAndWaitAsync("name Alice", "Welcome, Alice! Type help to get started.", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("Welcome, Alice! Type help to get started.");
    }

    [Fact, TestPriority(3)]
    public async Task NameCommandRejectsNamesThatAreTooLong()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAtPromptAsync();

        // Act
        var response = await c.SendAndWaitAsync("name ThisNameIsWayTooLongToBeValid", "That name is a bit long (max 20).", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("That name is a bit long (max 20).");
    }

    [Fact, TestPriority(4)]
    public async Task CanSayAfterNaming()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("Bob", ct: TestContext.Current.CancellationToken);

        // Act
        var response = await c.SendAndWaitAsync("say Hello everyone!", "You say: Hello everyone!", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("You say: Hello everyone!");
    }

    [Fact, TestPriority(5)]
    public async Task CanSayWithLeadingApostropheAfterNaming()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("Bob", ct: TestContext.Current.CancellationToken);

        // Act
        var response = await c.SendAndWaitAsync("'Hello everyone!", "You say: Hello everyone!", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("You say: Hello everyone!");
    }

    [Fact, TestPriority(6)]
    public async Task CanSayWithoutLeadingApostropheAfterNaming()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("Bob", ct: TestContext.Current.CancellationToken);

        // Act
        var response = await c.SendAndWaitAsync("Hello everyone!", "You say: Hello everyone!", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("You say: Hello everyone!");
    }

    [Fact, TestPriority(7)]
    public async Task HelpCommandShowsGeneralHelpListing()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("TestUser", ct: TestContext.Current.CancellationToken);

        // Act
        await c.SendLineAsync("help", TestContext.Current.CancellationToken);
        var lines = await c.ExpectAsync(RealmClient.DefaultTimeout, "Commands:", "help", "look", "name", "say", "who", "Try: help");

        // Assert
        lines[0].ShouldContainWithoutAnsi("Commands:");
        var commandSection = string.Join("\n", lines);
        commandSection.ShouldContainWithoutAnsi("help");
        commandSection.ShouldContainWithoutAnsi("look");
        commandSection.ShouldContainWithoutAnsi("name");
        commandSection.ShouldContainWithoutAnsi("say");
        commandSection.ShouldContainWithoutAnsi("who");
        lines[^1].ShouldContainWithoutAnsi("Try: help");
    }

    [Fact, TestPriority(8)]
    public async Task HelpCommandShowsCommandsInAlphabeticalOrder()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("TestUser", ct: TestContext.Current.CancellationToken);

        // Act
        await c.SendLineAsync("help", TestContext.Current.CancellationToken);
        var lines = await c.ExpectAsync(RealmClient.DefaultTimeout, "Commands:", "help", "look", "name", "say", "who");

        // Assert - verify alphabetical ordering by checking that commands appear in sequence
        var fullOutput = string.Join("\n", lines.Select(Sanitizer.StripAnsi));
        var helpIndex = fullOutput.IndexOf("help - ", StringComparison.OrdinalIgnoreCase);
        var lookIndex = fullOutput.IndexOf("look - ", StringComparison.OrdinalIgnoreCase);
        var nameIndex = fullOutput.IndexOf("name - ", StringComparison.OrdinalIgnoreCase);
        var sayIndex = fullOutput.IndexOf("say - ", StringComparison.OrdinalIgnoreCase);
        var whoIndex = fullOutput.IndexOf("who - ", StringComparison.OrdinalIgnoreCase);

        helpIndex.ShouldBeGreaterThan(-1);
        lookIndex.ShouldBeGreaterThan(helpIndex);
        nameIndex.ShouldBeGreaterThan(lookIndex);
        sayIndex.ShouldBeGreaterThan(nameIndex);
        whoIndex.ShouldBeGreaterThan(sayIndex);
    }

    [Fact, TestPriority(9)]
    public async Task HelpCommandShowsCommandSpecificHelp()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("TestUser", ct: TestContext.Current.CancellationToken);

        // Act
        await c.SendLineAsync("help say", TestContext.Current.CancellationToken);
        var lines = await c.ExpectAsync(RealmClient.DefaultTimeout, "say", "Speak to everyone in the room", "Usage:");

        // Assert
        var fullOutput = string.Join("\n", lines);
        fullOutput.ShouldContainWithoutAnsi("say");
        fullOutput.ShouldContainWithoutAnsi("Speak to everyone in the room");
        fullOutput.ShouldContainWithoutAnsi("Usage:");
        fullOutput.ShouldContainWithoutAnsi("say <message>");
    }

    [Fact, TestPriority(10)]
    public async Task HelpCommandShowsErrorForInvalidCommand()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("TestUser", ct: TestContext.Current.CancellationToken);

        // Act
        var response = await c.SendAndWaitAsync("help invalidcommand", "No help for", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContainWithoutAnsi("No help for 'invalidcommand'");
    }

    [Fact, TestPriority(11)]
    public async Task LookCommandShowsRoomDescriptionAndOtherPlayers()
    {
        // Arrange
        await using var c = await RealmClient.ConnectAndNameAsync("TestUser", ct: TestContext.Current.CancellationToken);

        // Act
        await c.SendLineAsync("look", TestContext.Current.CancellationToken);
        var lines = await c.ExpectAsync(RealmClient.DefaultTimeout, "You are in a quiet, featureless void.", "Faint echoes hint at places not yet built.", "Also here:", "(no one)");

        // Assert
        var fullOutput = string.Join("\n", lines);
        fullOutput.ShouldContainWithoutAnsi("You are in a quiet, featureless void.");
        fullOutput.ShouldContainWithoutAnsi("Faint echoes hint at places not yet built.");
        fullOutput.ShouldContainWithoutAnsi("Also here:");
        fullOutput.ShouldContainWithoutAnsi("(no one)");
    }

    [Fact, TestPriority(12)]
    public async Task WhoCommandListsConnectedPlayers()
    {
        // Arrange
        await using var c1 = await RealmClient.ConnectAndNameAsync("Alice", ct: TestContext.Current.CancellationToken);
        await using var c2 = await RealmClient.ConnectAndNameAsync("Bob", ct: TestContext.Current.CancellationToken);

        // Act
        await c1.SendLineAsync("who", TestContext.Current.CancellationToken);
        var lines = await c1.ExpectAsync(RealmClient.DefaultTimeout, "Who:", " - Alice", " - Bob");

        // Assert
        var fullOutput = string.Join("\n", lines);
        fullOutput.ShouldContainWithoutAnsi("Who:");
        fullOutput.ShouldContainWithoutAnsi(" - Alice");
        fullOutput.ShouldContainWithoutAnsi(" - Bob");
    }

    [Fact, TestPriority(13)]
    public async Task TellCommandSendsPrivateMessageBetweenPlayers()
    {
        // Arrange
        await using var c1 = await RealmClient.ConnectAndNameAsync("Alice", ct: TestContext.Current.CancellationToken);
        await using var c2 = await RealmClient.ConnectAndNameAsync("Bob", ct: TestContext.Current.CancellationToken);

        // Act
        var sendResponse = await c1.SendAndWaitAsync("tell Bob Hello Bob!", "You tell Bob: Hello Bob!", TestContext.Current.CancellationToken);
        var receiveLines = await c2.ExpectAsync(RealmClient.DefaultTimeout, "Message from Alice: Hello Bob!");

        // Assert
        sendResponse.ShouldContainWithoutAnsi("You tell Bob: Hello Bob!");
        receiveLines[0].ShouldContainWithoutAnsi("Message from Alice: Hello Bob!");
    }
}
