#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// DB layer
using AnimaBattler.Data;

// Runtime models
using AnimaBattler.Core.Combat;
using AnimaBattler.Core.Anima;
using AnimaBattler.Core.Enemy;

// helpful alias for runtime enemy skill
using ESkill = AnimaBattler.Core.Enemy.EnemySkill;

var services = new ServiceCollection()
    .AddDbContext<GameDbContext>(o =>
        o.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                    ?? "Host=localhost;Port=5432;Database=petccg;Username=postgres;Password=postgres"))
    .BuildServiceProvider();

using var scope = services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

// -- Load units from DB -------------------------------------------------------
var anima = await LoadTestAnimaAsync(db);
var enemy = await LoadTestEnemyAsync(db);

// -- Battle sim (alt turns, max 8) -------------------------------------------
Console.WriteLine("=== Test Battle ===");
PrintState();

const int MAX_TURNS = 8;
for (int t = 1; t <= MAX_TURNS && !anima.IsDefeated && !enemy.IsDefeated; t++)
{
    Console.WriteLine($"\n-- Turn {t} --");

    // Anima acts
    if (!anima.IsDefeated)
    {
        var aSkill = ChooseFirstUsable(anima.Skills);
        ResolveSkill("ANIMA", anima, enemy, aSkill);
    }

    // Enemy acts
    if (!enemy.IsDefeated)
    {
        var eSkill = enemy.ConsumeNextSkill(); // runtime Enemy.Skill
        // Reuse same resolver by mapping Enemy.Skill → Anima.Skill shape
        var mapped = MapEnemySkillToAnimaSkill(eSkill);
        ResolveSkill("ENEMY", enemy, anima, mapped);
    }

    PrintState();
}

// Result
Console.WriteLine("\n=== Result ===");
Console.WriteLine(anima.IsDefeated ? "Enemy wins." :
                  enemy.IsDefeated ? "Anima wins!" : "Reached turn limit (draw).");

// ========================== helpers =========================================

void PrintState()
{
    Console.WriteLine(
        $"ANIMA {anima.Id}: HP {anima.CurrentHp}/{anima.BaseHp} | Shield {anima.CurrentShield}   ||   " +
        $"ENEMY {enemy.Id}: HP {enemy.CurrentHp}/{enemy.BaseHp} | Shield {enemy.CurrentShield}");
}

static Skill ChooseFirstUsable(IReadOnlyList<Skill> skills) => skills[0];

// unified resolver uses Anima.Skill record
static void ResolveSkill(string actorTag, Unit caster, Unit target, Skill skill)
{
    switch (skill.Type)
    {
        case PartType.Attack:
        {
            var dmg = CombatMath.CalculateDamage(caster, skill.BaseDamage);
            var hpLost = target.ReceiveDamage(dmg);
            Console.WriteLine($"{actorTag} uses {skill.Name}: deals {hpLost} damage.");
            break;
        }
        case PartType.Buff:
        {
            var shield = CombatMath.CalculateShield(skill.EffectValue);
            caster.AddShield(shield);
            Console.WriteLine($"{actorTag} uses {skill.Name}: gains {shield} shield.");
            break;
        }
        case PartType.Heal:
        {
            var amount = CombatMath.CalculateShield(skill.EffectValue);
            // For now we treat all heals as self-heal in this 1v1 demo.
            // (If you add teams, branch on skill.Target to heal allies collection.)
            var healed = HealUnit(caster, amount);
            Console.WriteLine($"{actorTag} uses {skill.Name}: heals {healed} HP.");
            break;
        }
        case PartType.Debuff:
        {
            // not implemented yet; log only
            Console.WriteLine($"{actorTag} uses {skill.Name}: debuff (no effect in stub).");
            break;
        }
        default:
            Console.WriteLine($"{actorTag} uses {skill.Name}: (no-op).");
            break;
    }
}

// We didn't add a public Heal method on Unit; do a small local helper.
// (If you add Unit.Heal(int) later, replace this with that call.)
static int HealUnit(Unit u, int amount)
{
    if (amount <= 0) return 0;
    var before = u.CurrentHp;
    var after = Math.Min(u.BaseHp, before + amount);

    // set protected setter via reflection (only in this console demo)
    var prop = typeof(Unit).GetProperty(nameof(Unit.CurrentHp), BindingFlags.Instance | BindingFlags.Public)!;
    prop.SetValue(u, after);
    return after - before;
}

static Skill MapEnemySkillToAnimaSkill(ESkill s) => new()
{
    Code = s.Code,
    Name = s.Name,
    Type = s.Type,
    BaseDamage = s.BaseDamage,
    EffectValue = s.EffectValue,
    DurationTurns = s.DurationTurns,
    Target = s.Target,
    Description = s.Description
};

// ----------------------- Loaders (DB → runtime) ------------------------------

static async Task<AnimaBattler.Core.Anima.Anima> LoadTestAnimaAsync(GameDbContext db)
{
    // Prefer Gray → Red → Green
    var aRow =
        await db.Animas.AsNoTracking().FirstOrDefaultAsync(a => a.Color == AnimaBattler.Core.Anima.Color.Gray) ??
        await db.Animas.AsNoTracking().FirstOrDefaultAsync(a => a.Color == AnimaBattler.Core.Anima.Color.Red) ??
        await db.Animas.AsNoTracking().FirstOrDefaultAsync(a => a.Color == AnimaBattler.Core.Anima.Color.Green)
        ?? throw new InvalidOperationException("No Anima rows found. Run AnimaSeeder.");

    var arche = await db.Archetypes.AsNoTracking().FirstAsync(x => x.Id == aRow.ArchetypeId);

    // Skills: from assigned codes if present; else choose any 2 random
    List<SkillEntity> chosenSkills;
    var codes = (aRow.AssignedSkillCodes ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (codes.Length > 0)
    {
        chosenSkills = await db.Skills.AsNoTracking()
            .Where(s => codes.Contains(s.Code))
            .ToListAsync();
    }
    else
    {
        chosenSkills = await db.Skills.AsNoTracking()
            .OrderBy(_ => Guid.NewGuid())
            .Take(2)
            .ToListAsync();
    }

    var runtimeSkills = chosenSkills.Select(s => new AnimaBattler.Core.Anima.Skill
    {
        Code = s.Code,
        Name = s.Name,
        Type = s.Type,
        BaseDamage = s.BaseDamage,
        EffectValue = s.EffectValue,
        DurationTurns = s.DurationTurns,
        Target = s.Target,
        Description = s.Description ?? string.Empty
    }).ToList();

    var runtimeArchetype = new AnimaBattler.Core.Anima.Archetype
    {
        Code = $"a_{aRow.Color.ToString().ToLowerInvariant()}_{aRow.Level}",
        Color = aRow.Color,
        Level = aRow.Level,
        Hp = aRow.Hp > 0 ? aRow.Hp : 90,
        DamageMultiplier = aRow.DamageMultiplier > 0 ? aRow.DamageMultiplier : 1.00m,
        Description = arche.Description ?? string.Empty
    };

    // Parts are optional for this demo: use empty list
    return new AnimaBattler.Core.Anima.Anima(runtimeArchetype, Enumerable.Empty<AnimaBattler.Core.Anima.Part>(), runtimeSkills);
}

static async Task<AnimaBattler.Core.Enemy.Enemy> LoadTestEnemyAsync(GameDbContext db)
{
    var eRow = await db.Enemies.AsNoTracking().FirstOrDefaultAsync()
        ?? throw new InvalidOperationException("No Enemy rows found. Run EnemySeeder.");

    var eSkillEntities = await db.EnemySkills.AsNoTracking()
        .OrderBy(_ => Guid.NewGuid())
        .Take(2)
        .ToListAsync();

    if (eSkillEntities.Count == 0)
        throw new InvalidOperationException("No enemy_skills found. Run EnemySkillSeeder.");

    var runtimeArchetype = new AnimaBattler.Core.Enemy.Archetype
    {
        Code = eRow.Code,
        Color = eRow.Color,
        Level = eRow.Level,
        Hp = eRow.Hp,
        DamageMultiplier = eRow.DamageMultiplier,
        Description = eRow.Description ?? string.Empty
    };

    var rotation = eSkillEntities.Select(s => new ESkill
    {
        Code = s.Code,
        Name = s.Name,
        Type = s.Type,
        BaseDamage = s.BaseDamage,
        EffectValue = s.EffectValue,
        DurationTurns = s.DurationTurns,
        Target = s.Target,
        Description = s.Description ?? string.Empty
    }).ToList();

    return new AnimaBattler.Core.Enemy.Enemy(runtimeArchetype, rotation, eRow.Role);
}
