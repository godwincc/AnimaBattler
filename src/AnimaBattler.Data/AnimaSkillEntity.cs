#nullable enable
using System;

namespace AnimaBattler.Data;

public sealed class AnimaSkillEntity
{
    public long AnimaId { get; set; }
    public AnimaEntity Anima { get; set; } = null!;

    public long SkillId { get; set; }
    public SkillEntity Skill { get; set; } = null!;

    // Optional gameplay metadata
    public bool IsEquipped { get; set; } = true;   // in current loadout
    public int OrderIndex { get; set; } = 0;       // play/priority order (0..N-1)
    public DateTime LearnedAtUtc { get; set; } = DateTime.UtcNow;
}
