using System.Text.RegularExpressions;

namespace TheFracturedRealm;

internal static class Sanitizer
{
    private static readonly Regex Csi = new(@"\u001b\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled);
    private static readonly Regex Osc = new(@"\u001b\][^\a\u001b]*(\a|\u001b\\)", RegexOptions.Compiled);
    private static readonly Regex EscSingles = new(@"\u001b[@-Z\\-_]", RegexOptions.Compiled);
    private static readonly Regex C0 = new(@"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", RegexOptions.Compiled);
    public static string OneLine(string s) => s.Replace("\r", "", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
    public static string StripAnsi(string s)
        => C0.Replace(EscSingles.Replace(Osc.Replace(Csi.Replace(s, ""), ""), ""), "");
    public static string SafeText(string s) => StripAnsi(OneLine(s));
}
