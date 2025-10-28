#nullable enable
using AnimaBattler.Core.Anima; // reuses Color enum

namespace AnimaBattler.Core.Enemy;

/// <summary>
/// Blueprint for an enemy at a given level (deterministic stats).
/// This is the runtime mirror of whatever DB "enemy archetype template" row you'll have later.
/// </summary>
public sealed record Archetype
{
    public required string Code { get; init; }             // e.g., "e_gray_3"
    public required Color Color { get; init; }             // Gray/Red/Green/...
    public required int Level { get; init; }               // 1..N
    public required int Hp { get; init; }                  // base HP
    public required decimal DamageMultiplier { get; init; } // e.g., 1.00..1.30
    public string Description { get; init; } = string.Empty;
}
