namespace TheFracturedRealm.Application.Abstractions.Data.Models;

public sealed class CharacterReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
