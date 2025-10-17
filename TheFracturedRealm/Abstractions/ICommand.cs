namespace TheFracturedRealm.Abstractions;

public interface ICommand
{
    string Name { get; }
    string[] Aliases => Array.Empty<string>();
    string Usage { get; }
    string Summary { get; }
    bool Matches(string line)
    {
        var verb = ExtractVerb(line);
        if (verb.Equals(Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        foreach (var a in Aliases)
        {
            if (verb.Equals(a, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
    static string ExtractVerb(string line)
    {
        var i = line.IndexOf(' ');
        return i < 0 ? line : line[..i];
    }
    Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct);
}
