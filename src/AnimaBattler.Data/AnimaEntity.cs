#nullable enable
using AnimaBattler.Core.Anima;

namespace AnimaBattler.Data;

public sealed class AnimaEntity
{
    public long Id { get; set; }

    public required string Name { get; set; }
    public Color Color { get; set; }
    public long ArchetypeId { get; set; }

    public int Level { get; set; }
    public int Hp { get; set; }
    public decimal DamageMultiplier { get; set; }

    public string? Description { get; set; }

    // comma-separated skill and part codes for quick prototyping
    public string? AssignedSkillCodes { get; set; }
    public string? AssignedPartCodes { get; set; }
}
