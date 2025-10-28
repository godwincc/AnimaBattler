#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnimaBattler.Core.Fusion
{
    // ─────────────────────────────────────────────────────────────────────────
    // Core Types
    // ─────────────────────────────────────────────────────────────────────────
    public enum ColorArchetype
    {
        Gray,   // Defense (finalized)
        Red,    // Physical DPS (finalized)
        Green,  // Support/Healer/Speed (TBD details)
        Blue,
        Yellow,
        Purple
    }

    /// <summary>
    /// Single or hybrid class. Hybrids are ordered pairs, e.g., (Red, Gray) == (Gray, Red).
    /// </summary>
    public readonly struct FusionClass : IEquatable<FusionClass>
    {
        public readonly ColorArchetype? Single;
        public readonly (ColorArchetype A, ColorArchetype B)? Hybrid;

        private FusionClass(ColorArchetype single)
        {
            Single = single;
            Hybrid = null;
        }
        private FusionClass(ColorArchetype a, ColorArchetype b)
        {
            if (a.CompareTo(b) <= 0) Hybrid = (a, b); else Hybrid = (b, a);
            Single = null;
        }

        public static FusionClass Of(ColorArchetype c) => new(c);
        public static FusionClass Of(ColorArchetype a, ColorArchetype b) => a == b ? new FusionClass(a) : new FusionClass(a, b);

        public bool IsSingle => Single.HasValue;
        public bool IsHybrid => Hybrid.HasValue;

        public override string ToString() => IsSingle ? Single!.Value.ToString() : $"{Hybrid!.Value.A}-{Hybrid!.Value.B}";
        public bool Equals(FusionClass other) => Single == other.Single && Hybrid == other.Hybrid;
        public override bool Equals(object? obj) => obj is FusionClass f && Equals(f);
        public override int GetHashCode() => HashCode.Combine(Single, Hybrid?.A, Hybrid?.B);
    }

    public enum PartSlot { A, B, C, D, E, F }
    public enum PartKind { Active, Passive, PassiveDeathTrigger }

    public sealed class PartRef
    {
        public required PartSlot Slot { get; init; }
        /// <summary>Internal ID of the specific variant (e.g., "Gray-A-2" mapped to int ID in your DB).</summary>
        public required int VariantId { get; init; }
        public required PartKind Kind { get; init; }
    }

    public sealed class Creature
    {
        public required Guid Id { get; init; }
        public required FusionClass Class { get; init; }

        // Multipliers used by combat cards (universal system)
        public required decimal DamageMult { get; init; }
        public required decimal DefenseMult { get; init; }
        public required int Speed { get; init; } // integer speed tier (e.g., 1/3/5 for pure; hybrids get middle)

        // Parts: exactly one per slot
        public required PartRef PartA { get; init; }
        public required PartRef PartB { get; init; }
        public required PartRef PartC { get; init; }
        public required PartRef PartD { get; init; }
        public required PartRef PartE { get; init; }
        public required PartRef PartF { get; init; }

        public IEnumerable<PartRef> AllParts()
        {
            yield return PartA; yield return PartB; yield return PartC;
            yield return PartD; yield return PartE; yield return PartF;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Balance Tables (can be moved to data-driven later)
    // ─────────────────────────────────────────────────────────────────────────
    public static class ArchetypeBalance
    {
        // Baselines (feel free to tweak). We finalized Gray + Red philosophy earlier.
        // NOTE: These are *multipliers* applied to card base values.
        private static readonly Dictionary<ColorArchetype, (decimal dmg, decimal def, int speed)> _base = new()
        {
            [ColorArchetype.Gray]   = (dmg: 0.9m, def: 1.5m, speed: 1),  // Tanky, slow
            [ColorArchetype.Red]    = (dmg: 1.6m, def: 0.9m, speed: 5),  // DPS, fast
            [ColorArchetype.Green]  = (dmg: 1.0m, def: 1.0m, speed: 5),  // (Tentative): Fast support
            [ColorArchetype.Blue]   = (dmg: 1.2m, def: 1.1m, speed: 3),
            [ColorArchetype.Yellow] = (dmg: 1.1m, def: 1.0m, speed: 3),
            [ColorArchetype.Purple] = (dmg: 1.3m, def: 0.95m, speed: 3),
        };

        public static (decimal dmg, decimal def, int speed) Get(ColorArchetype c) => _base[c];

        /// <summary>
        /// Hybrid bonus rule: average parents then add a small synergy bump.
        /// </summary>
        public static (decimal dmg, decimal def, int speed) HybridOf(ColorArchetype a, ColorArchetype b)
        {
            var (ad, ae, aspeed) = Get(a);
            var (bd, be, bspeed) = Get(b);

            var dmg   = Math.Round(((ad + bd) / 2m) + 0.05m, 2); // +0.05 synergy
            var def   = Math.Round(((ae + be) / 2m) + 0.05m, 2);
            var speed = (int)Math.Round((aspeed + bspeed) / 2.0); // hybrids get the middle tier

            return (dmg, def, speed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Deterministic RNG (hash-of-inputs)
    // ─────────────────────────────────────────────────────────────────────────
    internal static class Deterministic
    {
        /// <summary>
        /// Returns a deterministic integer in [0, maxExclusive) based on the inputs.
        /// No global RNG: pure function of parent IDs + seed + context key.
        /// </summary>
        public static int Pick(string contextKey, Guid parentAId, Guid parentBId, int maxExclusive, int? seed = null)
        {
            var sb = new StringBuilder();
            sb.Append(contextKey).Append('|')
              .Append(parentAId.ToString("N")).Append('|')
              .Append(parentBId.ToString("N")).Append('|')
              .Append(seed?.ToString() ?? "0");

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            // convert first 4 bytes to int
            var val = BitConverter.ToInt32(hash, 0);
            if (val < 0) val = ~val;
            return val % maxExclusive;
        }

        /// <summary>Returns true deterministically with probability (num/den).</summary>
        public static bool Chance(string key, Guid a, Guid b, int num, int den, int? seed = null)
            => Pick(key, a, b, den, seed) < num;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Part Inheritance Policy
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class FusionRules
    {
        /// <summary>If true, allow hybrid classes; if false, choose one parent class only.</summary>
        public bool EnableHybrids { get; init; } = true;

        /// <summary>Nominal hybrid rate when parents are different (e.g., 20% of outcomes).</summary>
        public int HybridRateNumerator { get; init; } = 1;
        public int HybridRateDenominator { get; init; } = 5;

        /// <summary>Deterministic seed to vary outcomes across shards/servers. Optional.</summary>
        public int? Seed { get; init; }

        /// <summary>Priority if both E and F are death-triggers after inheritance. true => keep E, false => keep F.</summary>
        public bool PreferEWhenBothDeathTriggers { get; init; } = true;
    }

    public sealed class ParentView
    {
        public required Guid Id { get; init; }
        public required FusionClass Class { get; init; }
        public required PartRef PartA { get; init; }
        public required PartRef PartB { get; init; }
        public required PartRef PartC { get; init; }
        public required PartRef PartD { get; init; }
        public required PartRef PartE { get; init; }
        public required PartRef PartF { get; init; }

        public PartRef Get(PartSlot s) => s switch
        {
            PartSlot.A => PartA,
            PartSlot.B => PartB,
            PartSlot.C => PartC,
            PartSlot.D => PartD,
            PartSlot.E => PartE,
            PartSlot.F => PartF,
            _ => throw new ArgumentOutOfRangeException(nameof(s))
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fusion Engine
    // ─────────────────────────────────────────────────────────────────────────
    public static class FusionEngine
    {
        /// <summary>
        /// Fuse two parents into a new child, deterministically.
        /// </summary>
        public static Creature Fuse(ParentView A, ParentView B, FusionRules? rules = null)
        {
            rules ??= new FusionRules();

            // 1) Determine class
            var fclass = DetermineClass(A, B, rules);

            // 2) Compute multipliers from class
            var (dmg, def, speed) = fclass.IsSingle
                ? ArchetypeBalance.Get(fclass.Single!.Value)
                : ArchetypeBalance.HybridOf(fclass.Hybrid!.Value.A, fclass.Hybrid!.Value.B);

            // 3) Inherit parts per slot (A–F)
            var parts = new Dictionary<PartSlot, PartRef>();
            foreach (var slot in new[] { PartSlot.A, PartSlot.B, PartSlot.C, PartSlot.D, PartSlot.E, PartSlot.F })
            {
                parts[slot] = InheritSlot(slot, A, B, rules);
            }

            // 4) Enforce "only one death-trigger may activate".
            EnforceSingleDeathTrigger(parts, rules);

            // Build creature
            return new Creature
            {
                Id = Guid.NewGuid(),
                Class = fclass,
                DamageMult = dmg,
                DefenseMult = def,
                Speed = speed,

                PartA = parts[PartSlot.A],
                PartB = parts[PartSlot.B],
                PartC = parts[PartSlot.C],
                PartD = parts[PartSlot.D],
                PartE = parts[PartSlot.E],
                PartF = parts[PartSlot.F],
            };
        }

        private static FusionClass DetermineClass(ParentView A, ParentView B, FusionRules rules)
        {
            // If both parents are same *single* class, child is that class.
            if (A.Class.IsSingle && B.Class.IsSingle && A.Class.Single == B.Class.Single)
                return A.Class;

            // If hybrids are disabled, pick a parent deterministically (50/50)
            if (!rules.EnableHybrids)
            {
                var pick = Deterministic.Pick("class.pickParent", A.Id, B.Id, 2, rules.Seed);
                return pick == 0 ? A.Class : B.Class;
            }

            // Parents different: chance to create a hybrid of their *primaries*.
            var aPrim = A.Class.IsSingle ? A.Class.Single!.Value : A.Class.Hybrid!.Value.A;
            var bPrim = B.Class.IsSingle ? B.Class.Single!.Value : B.Class.Hybrid!.Value.A;
            if (aPrim == bPrim) return FusionClass.Of(aPrim); // degenerate case

            var hybrid = Deterministic.Chance("class.hybrid", A.Id, B.Id, rules.HybridRateNumerator, rules.HybridRateDenominator, rules.Seed);
            if (hybrid) return FusionClass.Of(aPrim, bPrim);

            // else pick one parent’s primary (deterministically)
            var pick2 = Deterministic.Pick("class.parentSingle", A.Id, B.Id, 2, rules.Seed);
            return FusionClass.Of(pick2 == 0 ? aPrim : bPrim);
        }

        private static PartRef InheritSlot(PartSlot slot, ParentView A, ParentView B, FusionRules rules)
        {
            // Deterministic 50/50 by slot
            var pick = Deterministic.Pick($"part.{slot}", A.Id, B.Id, 2, rules.Seed);
            var chosen = (pick == 0 ? A.Get(slot) : B.Get(slot));

            // (Hook) Later: add catalysts, purity bonuses, or class-weighted picks.
            return Clone(chosen);
        }

        private static void EnforceSingleDeathTrigger(Dictionary<PartSlot, PartRef> parts, FusionRules rules)
        {
            var e = parts[PartSlot.E];
            var f = parts[PartSlot.F];

            bool eDT = e.Kind == PartKind.PassiveDeathTrigger;
            bool fDT = f.Kind == PartKind.PassiveDeathTrigger;

            if (eDT && fDT)
            {
                if (rules.PreferEWhenBothDeathTriggers)
                {
                    // downgrade F to its nearest normal passive fallback
                    parts[PartSlot.F] = DowngradeToPassive(f);
                }
                else
                {
                    parts[PartSlot.E] = DowngradeToPassive(e);
                }
            }
        }

        // ── Helpers for cloning/downgrading parts (placeholder logic; wire to DB/IDs later)
        private static PartRef Clone(PartRef p) => new PartRef { Slot = p.Slot, VariantId = p.VariantId, Kind = p.Kind };

        /// <summary>
        /// Replace a death-trigger variant with a standard passive variant.
        /// In a data-driven setup, you’d look up the standard counterpart for the same slot/family.
        /// Here we simply map Kind to Passive; keep VariantId (or remap if you have a mapping function).
        /// </summary>
        private static PartRef DowngradeToPassive(PartRef p)
            => p.Kind == PartKind.PassiveDeathTrigger
                ? new PartRef { Slot = p.Slot, VariantId = p.VariantId, Kind = PartKind.Passive }
                : p;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Example Usage (you can move into tests)
    // ─────────────────────────────────────────────────────────────────────────
    public static class FusionExamples
    {
        public static Creature Example()
        {
            var parentA = new ParentView
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Class = FusionClass.Of(ColorArchetype.Gray),
                PartA = new PartRef { Slot = PartSlot.A, VariantId = 101, Kind = PartKind.Active },
                PartB = new PartRef { Slot = PartSlot.B, VariantId = 201, Kind = PartKind.Active },
                PartC = new PartRef { Slot = PartSlot.C, VariantId = 301, Kind = PartKind.Active },
                PartD = new PartRef { Slot = PartSlot.D, VariantId = 401, Kind = PartKind.Passive },
                PartE = new PartRef { Slot = PartSlot.E, VariantId = 501, Kind = PartKind.PassiveDeathTrigger },
                PartF = new PartRef { Slot = PartSlot.F, VariantId = 601, Kind = PartKind.Passive },
            };

            var parentB = new ParentView
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Class = FusionClass.Of(ColorArchetype.Red),
                PartA = new PartRef { Slot = PartSlot.A, VariantId = 102, Kind = PartKind.Active },
                PartB = new PartRef { Slot = PartSlot.B, VariantId = 202, Kind = PartKind.Active },
                PartC = new PartRef { Slot = PartSlot.C, VariantId = 302, Kind = PartKind.Active },
                PartD = new PartRef { Slot = PartSlot.D, VariantId = 402, Kind = PartKind.Passive },
                PartE = new PartRef { Slot = PartSlot.E, VariantId = 502, Kind = PartKind.Passive },
                PartF = new PartRef { Slot = PartSlot.F, VariantId = 602, Kind = PartKind.PassiveDeathTrigger },
            };

            var rules = new FusionRules
            {
                EnableHybrids = true,
                HybridRateNumerator = 1,   // 20% hybrid when different classes
                HybridRateDenominator = 5,
                Seed = 42,                 // optional shard seed for deterministic variety
                PreferEWhenBothDeathTriggers = true
            };

            return FusionEngine.Fuse(parentA, parentB, rules);
        }
    }
}
