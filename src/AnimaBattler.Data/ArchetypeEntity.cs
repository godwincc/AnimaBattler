#nullable enable
using AnimaBattler.Core.Anima;

namespace AnimaBattler.Data;

public sealed class ArchetypeEntity
{
    public long Id { get; set; }

    /// <summary>Archetype color identity (Gray, Red, Green, etc).</summary>
    public Color Color { get; set; }

    /// <summary>Display name, usually same as color but flexible for hybrids.</summary>
    public required string Name { get; set; }

    /// <summary>Base health stat.</summary>
    public int BaseHp { get; set; }

    /// <summary>Speed stat (for turn order).</summary>
    public int BaseSpeed { get; set; }

    /// <summary>Offensive scaling multiplier (1.0 = baseline).</summary>
    public decimal DamageMult { get; set; }

    /// <summary>Defensive scaling multiplier (1.0 = baseline).</summary>
    public double DefenseMult { get; set; }

    public string? Description { get; set; }
}
