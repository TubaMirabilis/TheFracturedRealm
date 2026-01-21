using Shouldly;

namespace TheFracturedRealm.FunctionalTests;

public static class AssertionHelpers
{
    public static void ShouldContainWithoutAnsi(this string line, string expected)
    {
        var plain = Sanitizer.StripAnsi(line);
        plain.ShouldContain(expected, Case.Insensitive);
    }
    public static void ShouldContainWithoutAnsi(this string line, string expected, Case caseSensitivity)
    {
        var plain = Sanitizer.StripAnsi(line);
        plain.ShouldContain(expected, caseSensitivity);
    }
}
