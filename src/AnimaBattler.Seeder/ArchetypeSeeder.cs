#nullable enable
using Microsoft.EntityFrameworkCore;
using AnimaBattler.Core.Anima;
using AnimaBattler.Data;

namespace AnimaBattler.Seeder;

public static class ArchetypeSeeder
{
    public static async Task SeedDefaultsAsync(GameDbContext db)
    {
        if (await db.Archetypes.AnyAsync()) return;

        var rows = new[]
        {
            new ArchetypeEntity {
                Color = Color.Gray, Name = "Gray",
                BaseHp = 85, BaseSpeed = 2,
                DamageMult = 0.95M, DefenseMult = 1.15,
                Description = "Tank/Support: block, mitigation, guard auras."
            },
            new ArchetypeEntity {
                Color = Color.Green, Name = "Green",
                BaseHp = 75, BaseSpeed = 5,
                DamageMult = 1.00M, DefenseMult = 1.00,
                Description = "Poison + Sustain + Growth; converts poison to burst."
            },
            new ArchetypeEntity {
                Color = Color.Red, Name = "Red",
                BaseHp = 70, BaseSpeed = 5,
                DamageMult = 1.12M, DefenseMult = 0.92,
                Description = "Aggression/Burst: high damage, lighter defense."
            },
        };

        db.Archetypes.AddRange(rows);
        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Archetypes seeded: {rows.Length}");
    }
}
