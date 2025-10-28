#nullable enable
using Microsoft.EntityFrameworkCore;
using AnimaBattler.Core.Anima;
using AnimaBattler.Data;
using System.Globalization;

namespace AnimaBattler.Seeder;

public static class SkillSeeder
{
    public static async Task SeedFromCsvAsync(GameDbContext db, string csvPath, bool upsert = true)
    {
        if (!File.Exists(csvPath))
            throw new FileNotFoundException($"CSV not found: {csvPath}");

        var lines = await File.ReadAllLinesAsync(csvPath);
        if (lines.Length <= 1) return;

        var header = SplitCsvLine(lines[0]);
        int idxCode         = Find(header, "Code",          true);
        int idxName         = Find(header, "Name", true);
        int idxEnergy       = Find(header, "Energy",          true);
        int idxType         = Find(header, "Type",          true);
        int idxBaseDamage   = Find(header, "BaseDamage",    true);
        int idxEffectValue  = Find(header, "EffectValue",   true);
        int idxDuration     = Find(header, "DurationTurns", true);
        int idxTarget       = Find(header, "Target",        true);
        int idxColor        = Find(header, "Color",         true);
        int idxDesc         = Find(header, "Description",   false);
        if (idxDesc < 0) idxDesc = Find(header, "Text", false);

        // cache archetypes by color to avoid N queries
        var archetypes = await db.Archetypes.AsNoTracking().ToListAsync();
        var archetypeByColor = archetypes.ToDictionary(a => a.Color, a => a);


        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = SplitCsvLine(line);

            string code  = Get(cells, idxCode);
            if (string.IsNullOrWhiteSpace(code)) continue;

            string name = Get(cells, idxName);
            string ener  = Get(cells, idxEnergy);
            string typeS = Get(cells, idxType);
            string dmgS  = Get(cells, idxBaseDamage);
            string effS  = Get(cells, idxEffectValue);
            string durS  = Get(cells, idxDuration);
            string tgtS  = Get(cells, idxTarget);
            string colS  = Get(cells, idxColor);
            string desc  = idxDesc >= 0 ? Get(cells, idxDesc) : string.Empty;

            var type = NormalizeType(typeS);
            var energy = ParseInt(ener);
            var target = ParseEnum<Target>(tgtS);
            var color  = ParseEnum<Color>(colS);
            var baseDmg = ParseInt(dmgS);            
            var effect  = ParseDecimal(effS);
            var dur     = ParseInt(durS);

            if (!archetypeByColor.TryGetValue(color, out var archetypeRow))
                throw new InvalidOperationException($"Archetype (Color={color}) not found. Seed archetypes first.");

            var existing = await db.Skills.FirstOrDefaultAsync(s => s.Code == code && s.ArchetypeId == archetypeRow.Id);
            if (existing is null)
            {
                db.Skills.Add(new SkillEntity
                {
                    ArchetypeId = archetypeRow.Id,
                    Code = code,
                    Name = name,
                    Energy = energy,
                    Type = type,
                    BaseDamage = baseDmg,
                    EffectValue = effect,
                    DurationTurns = dur,
                    Target = target,
                    Description = desc
                });
            }
            else if (upsert)
            {
                existing.Name = name;
                existing.Energy = energy;
                existing.Type = type;
                existing.BaseDamage = baseDmg;
                existing.EffectValue = effect;
                existing.DurationTurns = dur;
                existing.Target = target;
                existing.Description = desc;
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Skills imported: {await db.Skills.CountAsync()} total.");
    }

    // ---- helpers -----------------------------------------------------------

    private static int Find(string[] header, string name, bool required)
    {
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        if (required) throw new InvalidOperationException($"Column '{name}' not found in CSV.");
        return -1;
    }

    private static string Get(string[] cells, int idx)
        => (idx >= 0 && idx < cells.Length) ? cells[idx].Trim() : string.Empty;

    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"') { current.Append('\"'); i++; }
                else { inQuotes = !inQuotes; }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString()); current.Clear();
            }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var e)) return e;
        throw new FormatException($"Cannot parse '{value}' as {typeof(T).Name}.");
    }

    private static PartType NormalizeType(string cell)
    {
        var s = (cell ?? string.Empty).Trim().ToLowerInvariant();
        if (s.Contains("death")) return PartType.DeathTrigger;
        if (s.Contains("passive")) return PartType.Passive;
        if (s.Contains("debuff")) return PartType.Debuff;
        if (s.Contains("heal") || s.Contains("restore")) return PartType.Heal;
        if (s.Contains("buff") || s.Contains("shield") || s.Contains("protect")) return PartType.Buff;
        if (s.Contains("atk") || s.Contains("attack") || s.Contains("strike") || s.Contains("damage")) return PartType.Attack;
        return PartType.Buff;
    }

    private static int ParseInt(string s)
    {
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        throw new FormatException($"Invalid integer: '{s}'");
    }

    private static decimal ParseDecimal(string s)
    {
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return v;
        throw new FormatException($"Invalid decimal: '{s}'");
    }
}
