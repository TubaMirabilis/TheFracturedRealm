namespace TheFracturedRealm.Domain;

public class Character
{
    private Character(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
    private Character()
    {
    }
    public Guid Id { get; }
    public string Name { get; private set; } = null!;
    public static Character Create(string name) => new(name);
}
