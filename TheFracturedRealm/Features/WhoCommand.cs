using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class WhoCommand : ICommand
{
    public string Name => "who";
    public string Usage => "who";
    public string Summary => "List connected players.";
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        var list = ctx.World.SnapshotSessions()
            .Select(s => string.IsNullOrWhiteSpace(s.Name) ? s.ToString() : s.Name!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var body = list.Length == 0 ? "(nobody)" : string.Join("\n", list.Select(n => $" - {n}"));
        await ctx.Reply($"{Ansi.Underline}Who:{Ansi.Reset}\n{body}", ct);
    }
}
