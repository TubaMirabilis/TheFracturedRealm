using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class TellCommand : ICommand
{
    public string Name => "tell";
    public string[] Aliases => ["t"];
    public string Usage => "tell <name> <message>";
    public string Summary => "Send a private message to another player.";
    public async Task ExecuteAsync(CommandContext ctx, CommandInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.Session.Name))
        {
            await ctx.Reply($"Set your handle first: {Ansi.Yellow}name <yourname>{Ansi.Reset}", ct);
            return;
        }
        if (string.IsNullOrWhiteSpace(input.Args))
        {
            await ctx.Reply($"{Ansi.Yellow}Usage:{Ansi.Reset} {Usage}", ct);
            return;
        }
        var parts = input.Args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await ctx.Reply($"{Ansi.Yellow}Usage:{Ansi.Reset} {Usage}", ct);
            return;
        }
        var targetName = Sanitizer.SafeText(parts[0]);
        var message = Sanitizer.SafeText(parts[1]);
        if (string.IsNullOrWhiteSpace(message))
        {
            await ctx.Reply($"{Ansi.Red}Message cannot be empty.{Ansi.Reset}", ct);
            return;
        }
        var targetSession = ctx.World.SnapshotSessions()
            .FirstOrDefault(s => !ReferenceEquals(s, ctx.Session) &&
                !string.IsNullOrWhiteSpace(s.Name) &&
                string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase));
        if (targetSession is null)
        {
            await ctx.Reply($"{Ansi.Red}Player '{targetName}' not found or has no name set.{Ansi.Reset}", ct);
            return;
        }
        await ctx.Reply($"{Ansi.Magenta}You tell {targetSession.Name}:{Ansi.Reset} {message}", ct);
        targetSession.OutboundWriter.TryWrite(new OutboundMessage($"{Ansi.Magenta}Message from {ctx.Session.Name}:{Ansi.Reset} {message}"));
    }
}
