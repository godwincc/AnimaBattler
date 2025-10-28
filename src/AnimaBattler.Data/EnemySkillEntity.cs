#nullable enable
using AnimaBattler.Core.Anima;

namespace AnimaBattler.Data;

public sealed class EnemySkillEntity
{
    public long Id { get; set; }
    public required string Code { get; set; }          // e.g., "ENEMY_SLASH"
    public required string Name { get; set; }
   
    public PartType Type { get; set; }                 // Attack/Buff/Heal
    public int BaseDamage { get; set; }
    public decimal EffectValue { get; set; }
    public int DurationTurns { get; set; }
    public Target Target { get; set; }
    public string? Description { get; set; }
}
