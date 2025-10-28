#nullable enable
namespace AnimaBattler.Core.Anima;

/// <summary>
/// Data model for an Anima skill. Deterministic: flat values only;
/// scaling (if any) comes from the caster's DamageMultiplier.
/// </summary>
public sealed record Skill
{
    /// <summary>Unique code (e.g., "GRAY_ATK_002").</summary>
    public required string Code { get; init; }

    /// <summary>Display name for UI.</summary>
    public required string Name { get; init; }

    /// <summary>Functional category (reuses PartType for simplicity).</summary>
    public required PartType Type { get; init; }  // Attack/Heal/Buff/Debuff/Passive/DeathTrigger

   /// <summary>Energy required to execute skill.</summary>
    public int Energy { get; init; }

    /// <summary>Flat damage amount (0 if not an attack).</summary>
    public int BaseDamage { get; init; }

    /// <summary>
    /// Generic magnitude for non-damage effects.
    /// Examples: shield amount, slow tiers, % modifier (store as 0..1 if a percent).
    /// </summary>
    public decimal EffectValue { get; init; }

    /// <summary>Effect duration in turns (0 = instantaneous).</summary>
    public int DurationTurns { get; init; }

    /// <summary>Targeting hint for the engine.</summary>
    public Target Target { get; init; }

    /// <summary>Human-readable rules text.</summary>
    public string Description { get; init; } = string.Empty;
}
