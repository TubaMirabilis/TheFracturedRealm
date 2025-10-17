using System.Text;
using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm.Features;

public sealed class LookCommand : ICommand
{
    public string Name => "look";
    public string[] Aliases => new[] { "l" };
    public string Usage => "look";
    public string Summary => "Look around.";
    public async Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Ansi.Bold}You are in a quiet, featureless void.{Ansi.Reset}");
        sb.AppendLine("Faint echoes hint at places not yet built.");
        sb.AppendLine();
        sb.AppendLine($"{Ansi.Underline}Also here:{Ansi.Reset}");
        var others = ctx.World.Sessions
            .Where(s => s.Id != ctx.Session.Id)
            .Select(s => string.IsNullOrWhiteSpace(s.Name) ? s.ToString() : s.Name!)
            .DefaultIfEmpty("(no one)");
        foreach (var o in others)
        {
            sb.AppendLine($" - {o}");
        }
        await ctx.Reply(sb.ToString().TrimEnd(), ct);
    }
}
