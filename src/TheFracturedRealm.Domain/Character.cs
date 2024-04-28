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
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Age { get; set; } = 18;
    public int Level { get; set; }
    public int HitPoints { get; set; }
    public int Mana { get; set; }
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Constitution { get; set; }
    public int Charisma { get; set; }
    public int Experience { get; set; }
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Copper { get; set; }
    public int Platinum { get; set; }
    public int Weight { get; set; }
    public int Height { get; set; }
    public static Character Create(string name) => new(name);
}
