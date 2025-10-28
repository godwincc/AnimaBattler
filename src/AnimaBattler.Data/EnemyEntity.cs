
#nullable enable
using AnimaBattler.Core.Anima;
using AnimaBattler.Core.Enemy;

namespace AnimaBattler.Data;

public sealed class EnemyEntity
{
    public long Id { get; set; }

    public required string Code { get; set; }          // e.g., "e_gray_1"
    public Color Color { get; set; }                   // reuse Anima.Color
    public int Level { get; set; }
    public int Hp { get; set; }
    public decimal DamageMultiplier { get; set; }
    public Role Role { get; set; }                     // Frontliner/Dps/Support
    public string? Description { get; set; }
}
