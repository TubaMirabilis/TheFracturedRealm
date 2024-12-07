namespace TheFracturedRealm.Application.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CommandAliasAttribute : Attribute
{
    public string Alias { get; }
    public CommandAliasAttribute(string alias) => Alias = alias;
}
