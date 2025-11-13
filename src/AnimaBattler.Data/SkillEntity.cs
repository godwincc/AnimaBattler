#nullable enable
using AnimaBattler.Core.Anima;

namespace AnimaBattler.Data;

public sealed class SkillEntity
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    // NEW: which part slot this skill belongs to (A..F)
    public PartSlot Slot { get; set; }   // <— add this

    // You already have this as enum based on the error:
    public PartType Type { get; set; }   // Attack/Heal/Buff/Debuff/Passive/DeathTrigger

    public Target Target { get; set; }

    public long ArchetypeId { get; set; }
    public ArchetypeEntity Archetype { get; set; } = null!;

    public int BaseDamage { get; set; }
    public int BaseHeal { get; set; }        // if you added this; otherwise remove
    public decimal EffectValue { get; set; }
    public int DurationTurns { get; set; }
    public int Energy { get; set; }          // we added earlier
    public string? Description { get; set; }
}