#nullable enable
using Microsoft.EntityFrameworkCore;
using AnimaBattler.Core.Anima;
using AnimaBattler.Core.Enemy;
using AnimaBattler.Data;

namespace AnimaBattler.Seeder;

public static class EnemySeeder
{
    /// <summary>
    /// Seeds a single test enemy archetype (Gray Frontliner Lv1).
    /// Assign skills later once testing begins.
    /// </summary>
    public static async Task SeedDefaultsAsync(GameDbContext db)
    {
        if (await db.Enemies.AnyAsync()) return;

        var enemy = new EnemyEntity
        {
            Code = "e_gray_1",
            Color = Color.Gray,
            Level = 1,
            Hp = 120,
            DamageMultiplier = 1.0m,
            Role = Role.Frontliner,
            Description = "A basic Gray frontliner used for testing combat and skill logic."
        };

        db.Enemies.Add(enemy);
        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Enemy seeded: {enemy.Code}");
    }
}
