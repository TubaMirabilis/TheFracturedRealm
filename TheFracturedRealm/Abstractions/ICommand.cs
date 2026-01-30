namespace TheFracturedRealm.Abstractions;

internal interface ICommand
{
    string Name { get; }
    string[] Aliases => [];
    string Usage { get; }
    string Summary { get; }
    bool Matches(CommandInput input)
    {
        var verb = input.Verb;
        if (verb.Equals(Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return Aliases.Any(a => verb.Equals(a, StringComparison.OrdinalIgnoreCase));
    }
    Task ExecuteAsync(CommandContext ctx, CommandInput input, CancellationToken ct);
}
