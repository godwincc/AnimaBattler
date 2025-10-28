#nullable enable
using AnimaBattler.Core.Anima; // reuses Target enum
namespace AnimaBattler.Core.Enemy;

/// <summary>
/// Enemy skill datum. Deterministic: flat values only; scaling comes from the enemy's DamageMultiplier.
/// </summary>
public sealed record EnemySkill
{
    /// <summary>Unique internal code (e.g., "GRAY_ATK_002").</summary>
    public required string Code { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Functional category (Attack/Buff/Debuff).</summary>
    public required PartType Type { get; init; }  // reuse your existing enum

    /// <summary>Flat damage amount (0 if not applicable).</summary>
    public int BaseDamage { get; init; }

    /// <summary>Generic magnitude for non-damage effects (e.g., shield amount, slow tiers, % modifiers).</summary>
    public decimal EffectValue { get; init; }

    /// <summary>Effect duration in turns (0 = instantaneous).</summary>
    public int DurationTurns { get; init; }

    /// <summary>Targeting hint for the engine.</summary>
    public required Target Target { get; init; }

    /// <summary>Rules text for UI/debug.</summary>
    public string Description { get; init; } = string.Empty;
}
