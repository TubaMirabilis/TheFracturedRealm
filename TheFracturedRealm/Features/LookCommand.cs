using System.Globalization;
using System.Text;
using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

internal sealed class LookCommand : ICommand
{
    public string Name => "look";
    public string[] Aliases => ["l"];
    public string Usage => "look";
    public string Summary => "Look around.";
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"{Ansi.Bold}You are in a quiet, featureless void.{Ansi.Reset}");
        sb.AppendLine("Faint echoes hint at places not yet built.");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"{Ansi.Underline}Also here:{Ansi.Reset}");
        var others = ctx.World.SnapshotSessions()
            .Where(s => s.Id != ctx.Session.Id)
            .Select(s => string.IsNullOrWhiteSpace(s.Name) ? s.ToString() : s.Name!)
            .DefaultIfEmpty("(no one)");
        foreach (var o in others)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $" - {o}");
        }
        await ctx.Reply(sb.ToString().TrimEnd(), ct);
    }
}
