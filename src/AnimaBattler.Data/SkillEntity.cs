#nullable enable
using AnimaBattler.Core.Anima;

namespace AnimaBattler.Data;

public sealed class SkillEntity
{
    public long Id { get; set; }

    // Foreign key to the owning archetype (required)
    public long ArchetypeId { get; set; }
    public ArchetypeEntity Archetype { get; set; } = default!;

    public required string Code { get; set; }          // unique (or unique per archetype—see config)
    public required string Name { get; set; }
     public int Energy { get; set; }
    public PartType Type { get; set; }                 // Attack/Heal/Buff/Debuff/Passive/DeathTrigger
    public int BaseDamage { get; set; }
    public decimal EffectValue { get; set; }
    public int DurationTurns { get; set; }
    public Target Target { get; set; }
    public string? Description { get; set; }
}
