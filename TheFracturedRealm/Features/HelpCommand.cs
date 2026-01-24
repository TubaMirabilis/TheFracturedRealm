using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class HelpCommand : ICommand
{
    private readonly Func<IEnumerable<ICommand>> _getCommands;
    public HelpCommand(Func<IEnumerable<ICommand>> getCommands) => _getCommands = getCommands;
    public string Name => "help";
    public string Usage => "help [command]";
    public string Summary => "Show help for commands.";
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        var commands = _getCommands().ToArray();
        var parts = raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var lines = commands.OrderBy(c => c.Name)
                                .Select(c => $"{Ansi.Yellow}{c.Name}{Ansi.Reset} - {c.Summary}");
            await ctx.Reply($"Commands:\n{string.Join("\n", lines)}\n\nTry: {Ansi.Yellow}help say{Ansi.Reset}", ct);
            return;
        }
        var name = parts[1];
        var cmd = commands.FirstOrDefault(c => Matches(c, name));
        if (cmd is null)
        {
            await ctx.Reply($"No help for '{name}'.", ct);
            return;
        }
        await ctx.Reply($"{Ansi.Yellow}{cmd.Name}{Ansi.Reset}\n{cmd.Summary}\nUsage: {Ansi.Yellow}{cmd.Usage}{Ansi.Reset}", ct);
    }
    private static bool Matches(ICommand c, string name) =>
        c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
        c.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
}
