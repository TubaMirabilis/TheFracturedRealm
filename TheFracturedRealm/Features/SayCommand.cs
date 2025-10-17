using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

public sealed class SayCommand : ICommand
{
    public string Name => "say";
    public string[] Aliases => new[] { "'", "s" };
    public string Usage => "say <message>";
    public string Summary => "Speak to everyone in the room.";
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.Session.Name))
        {
            await ctx.Reply($"Set your handle first: {Ansi.Yellow}name <yourname>{Ansi.Reset}", ct);
            return;
        }
        var msg = Sanitizer.SafeText(ExtractMessage(raw));
        if (string.IsNullOrWhiteSpace(msg))
        {
            await ctx.Reply($"{Ansi.Yellow}Usage:{Ansi.Reset} {Usage}", ct);
            return;
        }
        await ctx.Reply($"{Ansi.Green}You say:{Ansi.Reset} {msg}", ct);
        await ctx.Broadcast($"{Ansi.Cyan}{ctx.Session}{Ansi.Reset} says: {msg}", except: ctx.Session, ct);
    }
    private static string ExtractMessage(string raw)
    {
        var i = raw.IndexOf(' ');
        if (i < 0)
        {
            return string.Empty;
        }
        return raw[(i + 1)..].Trim();
    }
}
