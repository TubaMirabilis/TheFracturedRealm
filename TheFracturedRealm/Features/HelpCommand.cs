using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class HelpCommand : ICommand
{
    private readonly CommandDispatcher _dispatcher;
    public HelpCommand(CommandDispatcher dispatcher) => _dispatcher = dispatcher;
    public string Name => "help";
    public string Usage => "help [command]";
    public string Summary => "Show help for commands.";
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        var parts = raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var lines = _dispatcher.Commands
                .OrderBy(c => c.Name)
                .Select(c => $"{Ansi.Yellow}{c.Name}{Ansi.Reset} - {c.Summary}");
            await ctx.Reply($"Commands:\n{string.Join("\n", lines)}\n\nTry: {Ansi.Yellow}help say{Ansi.Reset}", ct);
            return;
        }
        var name = parts[1];
        var cmd = _dispatcher.Commands.FirstOrDefault(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            c.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)));
        if (cmd is null)
        {
            await ctx.Reply($"No help for '{name}'.", ct);
            return;
        }
        await ctx.Reply($"{Ansi.Yellow}{cmd.Name}{Ansi.Reset}\n{cmd.Summary}\nUsage: {Ansi.Yellow}{cmd.Usage}{Ansi.Reset}", ct);
    }
}
