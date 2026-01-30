namespace TheFracturedRealm;

internal readonly record struct CommandInput(string Raw, string Verb, string Args)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Raw);
    public static CommandInput Parse(string raw)
    {
        raw = raw?.Trim() ?? string.Empty;
        if (raw.Length == 0)
        {
            return new CommandInput(string.Empty, string.Empty, string.Empty);
        }
        if (raw[0] == '\'')
        {
            var args = raw.Length == 1 ? string.Empty : raw[1..].TrimStart();
            return new CommandInput(raw, "say", args);
        }
        var i = raw.IndexOf(' ', StringComparison.Ordinal);
        if (i < 0)
        {
            return new CommandInput(raw, raw, string.Empty);
        }
        var verb = raw[..i];
        var argsPart = raw[(i + 1)..].TrimStart();
        return new CommandInput(raw, verb, argsPart);
    }
}
