#nullable enable
using System.Diagnostics.CodeAnalysis;
using AnimaBattler.Core.Combat;

namespace AnimaBattler.Core.Anima;

/// <summary>
/// Player-controlled battlefield unit. 
/// Composed of an Archetype, modular Parts, and a collection of Skills.
/// Deterministic — no RNG at runtime.
/// </summary>
public sealed class Anima : Unit
{
    public required Archetype Archetype { get; init; }
    public required IReadOnlyList<Part> Parts { get; init; }    // A–F
    public required IReadOnlyList<Skill> Skills { get; init; }  // derived from parts or loadout

    public override int BaseHp => Archetype.Hp;
    public override decimal DamageMultiplier => Archetype.DamageMultiplier;

    [SetsRequiredMembers]
    public Anima(Archetype archetype, IEnumerable<Part> parts, IEnumerable<Skill> skills, string? id = null)
    {
        Archetype = archetype;
        Parts = parts.ToList();
        Skills = skills.ToList();

        Id = id ?? $"{archetype.Code}#{Guid.NewGuid().ToString("N")[..6]}";
        CurrentHp = archetype.Hp;
    }

    /// <summary>
    /// Adds a shield to this Anima. 
    /// Shield cannot be negative; caps at a reasonable upper limit.
    /// </summary>
    public override void AddShield(int amount)
    {
        if (amount <= 0) return;
        CurrentShield = Math.Min(CurrentShield + amount, Archetype.Hp * 2); // soft cap at 2x HP
    }

    /// <summary>
    /// Applies a skill to a target and returns the total damage or shield applied.
    /// You can expand this later when implementing your combat engine.
    /// </summary>
    public int UseSkill(Skill skill, Unit target)
    {
        var result = 0;

        switch (skill.Type)
        {
            case PartType.Attack:
                result = Combat.CombatMath.CalculateDamage(this, skill.BaseDamage);
                target.ReceiveDamage(result);
                break;

            case PartType.Buff:
            case PartType.Heal:
                result = Combat.CombatMath.CalculateShield(skill.EffectValue);
                AddShield(result);
                break;

            // Debuff/Passive/DeathTrigger handled externally
        }

        return result;
    }
}
