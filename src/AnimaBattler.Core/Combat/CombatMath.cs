#nullable enable
namespace AnimaBattler.Core.Combat;

/// <summary>
/// Centralized deterministic math helpers for combat.
/// </summary>
public static class CombatMath
{
    /// <summary>
    /// Calculates attack damage using the attacker's DamageMultiplier.
    /// </summary>
    public static int CalculateDamage(Unit attacker, int baseDamage)
    {
        if (baseDamage <= 0) return 0;

        var scaled = baseDamage * attacker.DamageMultiplier;
        return (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates shield or heal amount.
    /// </summary>
    public static int CalculateShield(decimal effectValue)
    {
        if (effectValue <= 0) return 0;
        return (int)Math.Round(effectValue, MidpointRounding.AwayFromZero);
    }
}
