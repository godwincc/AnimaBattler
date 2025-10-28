#nullable enable
using System;
using System.Linq;
using AnimaBattler.Core.Fusion;
using Xunit;

namespace AnimaBattler.Tests.Fusion
{
    public class FusionEngineTests
    {
        // Fixed GUIDs to keep tests deterministic
        private static readonly Guid GA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid GB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private static ParentView MakeParent(
            Guid id,
            FusionClass fclass,
            (int A, int B, int C, int D, int E, int F)? variants = null,
            (PartKind A, PartKind B, PartKind C, PartKind D, PartKind E, PartKind F)? kinds = null)
        {
            variants ??= (101, 201, 301, 401, 501, 601);
            kinds ??= (PartKind.Active, PartKind.Active, PartKind.Active, PartKind.Passive, PartKind.Passive, PartKind.Passive);

            return new ParentView
            {
                Id = id,
                Class = fclass,
                PartA = new PartRef { Slot = PartSlot.A, VariantId = variants.Value.A, Kind = kinds.Value.A },
                PartB = new PartRef { Slot = PartSlot.B, VariantId = variants.Value.B, Kind = kinds.Value.B },
                PartC = new PartRef { Slot = PartSlot.C, VariantId = variants.Value.C, Kind = kinds.Value.C },
                PartD = new PartRef { Slot = PartSlot.D, VariantId = variants.Value.D, Kind = kinds.Value.D },
                PartE = new PartRef { Slot = PartSlot.E, VariantId = variants.Value.E, Kind = kinds.Value.E },
                PartF = new PartRef { Slot = PartSlot.F, VariantId = variants.Value.F, Kind = kinds.Value.F },
            };
        }

        [Fact]
        public void Fuse_SameClass_ProducesPureClassAndStats()
        {
            // Arrange
            var a = MakeParent(GA, FusionClass.Of(ColorArchetype.Gray));
            var b = MakeParent(GB, FusionClass.Of(ColorArchetype.Gray));

            var rules = new FusionRules { EnableHybrids = true, Seed = 42 };

            // Act
            var child = FusionEngine.Fuse(a, b, rules);

            // Assert
            Assert.True(child.Class.IsSingle);
            Assert.Equal(ColorArchetype.Gray, child.Class.Single);
            // From ArchetypeBalance: Gray => dmg 0.9, def 1.5, speed 1
            Assert.Equal(0.9m, child.DamageMult);
            Assert.Equal(1.5m, child.DefenseMult);
            Assert.Equal(1, child.Speed);
        }

        [Fact]
        public void Fuse_DifferentClasses_WithForcedHybrid_ProducesHybridAndAverages()
        {
            // Arrange: Gray + Red, force hybrid by setting rate 100%
            var a = MakeParent(GA, FusionClass.Of(ColorArchetype.Gray));
            var b = MakeParent(GB, FusionClass.Of(ColorArchetype.Red));
            var rules = new FusionRules
            {
                EnableHybrids = true,
                HybridRateNumerator = 5,   // 100% hybrid
                HybridRateDenominator = 5,
                Seed = 7
            };

            // Act
            var child = FusionEngine.Fuse(a, b, rules);

            // Assert: class is Gray-Red hybrid (orderless equals)
            Assert.True(child.Class.IsHybrid);
            var h = child.Class.Hybrid!.Value;
            Assert.True((h.A == ColorArchetype.Gray && h.B == ColorArchetype.Red) ||
                        (h.A == ColorArchetype.Red && h.B == ColorArchetype.Gray));

            // Stats: average + 0.05 synergy.
            // Gray: (0.9 dmg, 1.5 def, 1 spd), Red: (1.6 dmg, 0.9 def, 5 spd)
            // dmg = ((0.9 + 1.6)/2) + 0.05 = (2.5/2)+0.05 = 1.25+0.05 = 1.30
            // def = ((1.5 + 0.9)/2) + 0.05 = (2.4/2)+0.05 = 1.20+0.05 = 1.25
            // spd = round((1 + 5)/2) = round(3) = 3
            Assert.Equal(1.30m, child.DamageMult);
            Assert.Equal(1.25m, child.DefenseMult);
            Assert.Equal(3, child.Speed);
        }

        [Fact]
        public void Fuse_BothDeathTriggers_DowngradesOne_ToRespectSingleDT()
        {
            // Arrange: Both parents have E and F such that child could end up with both DTs.
            // We’ll set both parents to have E=DT and F=DT; the engine must downgrade one.
            var a = MakeParent(
                GA,
                FusionClass.Of(ColorArchetype.Red),
                kinds: (PartKind.Active, PartKind.Active, PartKind.Active, PartKind.Passive,
                        PartKind.PassiveDeathTrigger, PartKind.PassiveDeathTrigger));

            var b = MakeParent(
                GB,
                FusionClass.Of(ColorArchetype.Red),
                kinds: (PartKind.Active, PartKind.Active, PartKind.Active, PartKind.Passive,
                        PartKind.PassiveDeathTrigger, PartKind.PassiveDeathTrigger));

            var rules = new FusionRules
            {
                EnableHybrids = false,
                PreferEWhenBothDeathTriggers = true,
                Seed = 11
            };

            // Act
            var child = FusionEngine.Fuse(a, b, rules);

            // Assert: at most one DT among E/F
            var eIsDT = child.PartE.Kind == PartKind.PassiveDeathTrigger;
            var fIsDT = child.PartF.Kind == PartKind.PassiveDeathTrigger;
            Assert.False(eIsDT && fIsDT); // not both
        }

        [Fact]
        public void Fuse_IsDeterministic_ForSameParentsAndSeed()
        {
            // Arrange
            var a = MakeParent(GA, FusionClass.Of(ColorArchetype.Blue));
            var b = MakeParent(GB, FusionClass.Of(ColorArchetype.Purple));

            var rules = new FusionRules { EnableHybrids = true, Seed = 99, HybridRateNumerator = 1, HybridRateDenominator = 5 };

            // Act
            var c1 = FusionEngine.Fuse(a, b, rules);
            var c2 = FusionEngine.Fuse(a, b, rules);

            // Assert
            Assert.Equal(c1.Class.IsHybrid, c2.Class.IsHybrid);
            Assert.Equal(c1.Class.Single, c2.Class.Single);
            Assert.Equal(c1.Class.Hybrid, c2.Class.Hybrid);

            Assert.Equal(c1.DamageMult, c2.DamageMult);
            Assert.Equal(c1.DefenseMult, c2.DefenseMult);
            Assert.Equal(c1.Speed, c2.Speed);

            // Parts identical (by VariantId & Kind per slot)
            PartRef[] p1 = [c1.PartA, c1.PartB, c1.PartC, c1.PartD, c1.PartE, c1.PartF];
            PartRef[] p2 = [c2.PartA, c2.PartB, c2.PartC, c2.PartD, c2.PartE, c2.PartF];

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(p1[i].VariantId, p2[i].VariantId);
                Assert.Equal(p1[i].Kind, p2[i].Kind);
                Assert.Equal(p1[i].Slot, p2[i].Slot);
            }
        }
    }
}
