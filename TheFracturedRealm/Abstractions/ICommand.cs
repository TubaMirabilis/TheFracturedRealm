namespace TheFracturedRealm.Abstractions;

internal interface ICommand
{
    string Name { get; }
    string[] Aliases => [];
    string Usage { get; }
    string Summary { get; }
    bool Matches(string line)
    {
        var verb = ExtractVerb(line);
        if (verb.Equals(Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return Aliases.Any(a => verb.Equals(a, StringComparison.OrdinalIgnoreCase));
    }
    static string ExtractVerb(string line)
    {
        var i = line.IndexOf(' ', StringComparison.Ordinal);
        return i < 0 ? line : line[..i];
    }
    Task ExecuteAsync(CommandContext ctx, string raw, CancellationToken ct);
}
