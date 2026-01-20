using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class NameCommand : ICommand
{
    public string Name => "name";
    public string Usage => "name <yourname>";
    public string Summary => "Set your displayed handle.";
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        var parts = raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await ctx.Reply($"{Ansi.Yellow}Usage:{Ansi.Reset} {Usage}", ct);
            return;
        }
        var old = ctx.Session.Name;
        var proposed = Sanitizer.SafeText(parts[1]);
        if (string.IsNullOrWhiteSpace(proposed))
        {
            await ctx.Reply($"{Ansi.Red}That name is not valid.{Ansi.Reset}", ct);
            return;
        }
        if (ctx.World.SnapshotSessions().Any(s => !ReferenceEquals(s, ctx.Session) &&
            !string.IsNullOrWhiteSpace(s.Name) &&
            string.Equals(s.Name, proposed, StringComparison.OrdinalIgnoreCase)))
        {
            await ctx.Reply($"{Ansi.Red}That handle is already taken.{Ansi.Reset}", ct);
            return;
        }
        if (proposed.Length > 20)
        {
            await ctx.Reply($"{Ansi.Red}That name is a bit long (max 20).{Ansi.Reset}", ct);
            return;
        }
        ctx.Session.Name = proposed;
        if (string.IsNullOrEmpty(old))
        {
            await ctx.Reply($"Welcome, {Ansi.Cyan}{proposed}{Ansi.Reset}! Type {Ansi.Yellow}help{Ansi.Reset} to get started.", ct);
            await ctx.Broadcast($"{Ansi.Dim}* {proposed} materializes in a shimmer of light.{Ansi.Reset}", except: ctx.Session, ct);
            return;
        }
        await ctx.Reply($"Okay, you are now {Ansi.Cyan}{proposed}{Ansi.Reset}.", ct);
        await ctx.Broadcast($"{Ansi.Dim}* {old} is now known as {proposed}.{Ansi.Reset}", except: ctx.Session, ct);
    }
}
