#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AnimaBattler.Data;
// aliases – no full Core.Anima import
using AColor = AnimaBattler.Core.Anima.Color;
using PartType = AnimaBattler.Core.Anima.PartType;


namespace AnimaBattler.Seeder;

public static class AnimaSeeder
{
    /// <summary>
    /// Seeds three default Anima entities (Gray, Red, Green) for testing.
    /// Randomly assigns 2 skills if available. (No parts for now.)
    /// </summary>
    public static async Task SeedDefaultsAsync(GameDbContext db)
    {
        if (await db.Animas.AnyAsync()) return;

        var rng = new Random();

        // Load archetypes & skills (parts are intentionally omitted)
        var archetypes = await db.Archetypes.AsNoTracking().ToListAsync();
        var skills = await db.Skills.AsNoTracking().ToListAsync();

        if (archetypes.Count == 0 || skills.Count == 0)
        {
            Console.WriteLine("⚠️  Skipping AnimaSeeder — need archetypes and skills.");
            return;
        }

        string PickSkillCodes(Func<AnimaBattler.Data.SkillEntity, bool> predicate)
            => string.Join(',',
                skills.Where(predicate)
                      .OrderBy(_ => rng.Next())
                      .Take(2)
                      .Select(s => s.Code));

        var grayArche = archetypes.First(a => a.Color.ToString() == AColor.Gray.ToString());
        var redArche  = archetypes.First(a => a.Color.ToString() == AColor.Red.ToString());
        var greenArche= archetypes.First(a => a.Color.ToString() == AColor.Green.ToString());

        var rows = new[]
        {
            new AnimaEntity
            {
                Name = "Tektar",
                Color = (AnimaBattler.Core.Anima.Color)Enum.Parse(typeof(AnimaBattler.Core.Anima.Color), AColor.Gray.ToString(), true),
                ArchetypeId = grayArche.Id,
                Level = 1,
                Hp = 90,
                DamageMultiplier = 1.00m,
                Description = "Gray frontline Anima for testing.",
                AssignedSkillCodes = PickSkillCodes(s => s.Type is PartType.Attack or PartType.Buff),
                AssignedPartCodes = null // no parts yet
            },
            new AnimaEntity
            {
                Name = "Rexen",
                Color = (AnimaBattler.Core.Anima.Color)Enum.Parse(typeof(AnimaBattler.Core.Anima.Color), AColor.Red.ToString(), true),
                ArchetypeId = redArche.Id,
                Level = 1,
                Hp = 80,
                DamageMultiplier = 1.10m,
                Description = "Aggressive Red Anima for testing.",
                AssignedSkillCodes = PickSkillCodes(s => s.Type is PartType.Attack or PartType.Debuff),
                AssignedPartCodes = null
            },
            new AnimaEntity
            {
                Name = "Verdra",
                Color = (AnimaBattler.Core.Anima.Color)Enum.Parse(typeof(AnimaBattler.Core.Anima.Color), AColor.Green.ToString(), true),
                ArchetypeId = greenArche.Id,
                Level = 1,
                Hp = 85,
                DamageMultiplier = 1.00m,
                Description = "Support-oriented Green Anima for testing.",
                AssignedSkillCodes = PickSkillCodes(s => s.Type is PartType.Heal or PartType.Buff),
                AssignedPartCodes = null
            }
        };

        db.Animas.AddRange(rows); // <-- rows are AnimaEntity[] (no runtime type leakage)
        await db.SaveChangesAsync();
        Console.WriteLine($"✅  Animæ seeded: {rows.Length}");
    }
}
