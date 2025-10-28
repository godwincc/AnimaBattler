#nullable enable
using System.Diagnostics.CodeAnalysis;
using AnimaBattler.Core.Combat;

namespace AnimaBattler.Core.Enemy;

/// <summary>
/// Concrete battlefield enemy. Holds a deterministic skill rotation.
/// </summary>
public sealed class Enemy : Unit
{
    public required Archetype Template { get; init; }
    public required Role Role { get; init; }
    public required IReadOnlyList<EnemySkill> SkillRotation { get; init; }

    public override int BaseHp => Template.Hp;
    public override decimal DamageMultiplier => Template.DamageMultiplier;

    [SetsRequiredMembers]
    public Enemy(Archetype template, IEnumerable<EnemySkill> rotation, Role role = Role.Frontliner, string? id = null)
    {
        Template = template;
        Role = role;
        SkillRotation = rotation.ToList();
        if (SkillRotation.Count == 0) throw new ArgumentException("Enemy must have at least one skill in rotation.", nameof(rotation));

        Id = id ?? $"{template.Code}#{Guid.NewGuid().ToString("N")[..6]}";
        CurrentHp = template.Hp;
    }

    public EnemySkill PeekNextSkill() => SkillRotation[TurnCounter % SkillRotation.Count];

    public EnemySkill ConsumeNextSkill()
    {
        var s = SkillRotation[TurnCounter % SkillRotation.Count];
        NextTurn();
        return s;
    }
}
