using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class SayCommand : ICommand
{
    public string Name => "say";
    public string[] Aliases => ["'", "s"];
    public string Usage => "say <message>";
    public string Summary => "Speak to everyone in the room.";
    public bool Matches(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }
        if (line[0] == '\'')
        {
            return true;
        }
        return ((ICommand)this).Matches(line);
    }
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.Session.Name))
        {
            await ctx.Reply($"Set your handle first: {Ansi.Yellow}name <yourname>{Ansi.Reset}", ct);
            return;
        }
        var msg = Sanitizer.SafeText(ExtractMessageOrImplicit(raw));
        if (string.IsNullOrWhiteSpace(msg))
        {
            await ctx.Reply($"{Ansi.Yellow}Usage:{Ansi.Reset} {Usage}", ct);
            return;
        }
        var speaker = ctx.Session.Name!;
        await ctx.Reply($"{Ansi.Green}You say:{Ansi.Reset} {msg}", ct);
        await ctx.Broadcast($"{Ansi.Cyan}{speaker}{Ansi.Reset} says: {msg}", except: ctx.Session, ct);
    }
    private static string ExtractMessageOrImplicit(string raw)
    {
        raw = raw.Trim();
        if (raw.Length == 0)
        {
            return string.Empty;
        }

        if (raw[0] == '\'')
        {
            return raw[1..].TrimStart();
        }
        var i = raw.IndexOf(' ', StringComparison.Ordinal);
        if (i < 0)
        {
            return raw.Equals("say", StringComparison.OrdinalIgnoreCase) ||
                   raw.Equals("s", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : raw;
        }
        var verb = raw[..i];
        var rest = raw[(i + 1)..].TrimStart();
        if (verb.Equals("say", StringComparison.OrdinalIgnoreCase) ||
            verb.Equals("s", StringComparison.OrdinalIgnoreCase))
        {
            return rest;
        }
        return raw;
    }
}
