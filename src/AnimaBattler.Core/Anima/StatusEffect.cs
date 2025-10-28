#nullable enable
namespace AnimaBattler.Core.Anima;

/// <summary>
/// Represents a simple status like Poison, Regen, Shield, etc.
/// Stackable and can have duration (-1 = permanent).
/// </summary>
public sealed class StatusEffect
{
    public string Name { get; }
    public int Stacks { get; set; }
    public int Duration { get; set; } // -1 means infinite duration

    public StatusEffect(string name, int stacks, int duration = -1)
    {
        Name = name;
        Stacks = stacks;
        Duration = duration;
    }
}
