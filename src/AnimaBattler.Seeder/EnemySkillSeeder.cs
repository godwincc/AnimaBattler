#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AnimaBattler.Core.Anima;   // for PartType / Target enums
using AnimaBattler.Data;

namespace AnimaBattler.Seeder;

public static class EnemySkillSeeder
{
    /// <summary>
    /// Seeds a few baseline skills for enemies (Slash, Bolster, Recover).
    /// </summary>
    public static async Task SeedDefaultsAsync(GameDbContext db)
    {
        if (await db.EnemySkills.AnyAsync()) return;

        var rows = new[]
        {
            new EnemySkillEntity
            {
                Code = "ENEMY_SLASH",
                Name = "Slash",
                Type = PartType.Attack,
                BaseDamage = 40,
                EffectValue = 0,
                DurationTurns = 0,
                Target = Target.EnemyFront,
                Description = "Deals 40 flat damage to the front target."
            },
            new EnemySkillEntity
            {
                Code = "ENEMY_BOLSTER",
                Name = "Bolster",
                Type = PartType.Buff,
                BaseDamage = 0,
                EffectValue = 30m,
                DurationTurns = 2,
                Target = Target.Self,
                Description = "Gain 30 shield for 2 turns."
            },
            new EnemySkillEntity
            {
                Code = "ENEMY_RECOVER",
                Name = "Recover",
                Type = PartType.Heal,
                BaseDamage = 0,
                EffectValue = 25m,
                DurationTurns = 0,
                Target = Target.AllAllies, // 🔹 heals allies, not self
                Description = "Restore 25 HP to all allies."
            }
        };

        db.EnemySkills.AddRange(rows);
        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Enemy skills seeded: {rows.Length}");
    }
}
