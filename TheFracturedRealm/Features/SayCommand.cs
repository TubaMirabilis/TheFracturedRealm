using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class SayCommand : ICommand
{
    public string Name => "say";
    public string[] Aliases => ["s"];
    public string Usage => "say <message>";
    public string Summary => "Speak to everyone in the room.";
    public async Task ExecuteAsync(CommandContext ctx, CommandInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.Session.Name))
        {
            await ctx.Reply($"Set your handle first: {Ansi.Yellow}name <yourname>{Ansi.Reset}", ct);
            return;
        }
        var msg = Sanitizer.SafeText(
            input.Verb.Equals("say", StringComparison.OrdinalIgnoreCase) ||
            input.Verb.Equals("s", StringComparison.OrdinalIgnoreCase)
                ? input.Args
                : input.Raw
        );
        if (string.IsNullOrWhiteSpace(msg))
        {
            await ctx.Reply($"{Ansi.Yellow}Usage:{Ansi.Reset} {Usage}", ct);
            return;
        }
        var speaker = ctx.Session.Name!;
        await ctx.Reply($"{Ansi.Green}You say:{Ansi.Reset} {msg}", ct);
        await ctx.Broadcast($"{Ansi.Cyan}{speaker}{Ansi.Reset} says: {msg}", except: ctx.Session, ct);
    }
}
