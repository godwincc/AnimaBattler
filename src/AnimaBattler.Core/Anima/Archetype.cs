namespace AnimaBattler.Core.Anima;

    /// <summary>Blueprint for an Anima at a given level (deterministic stats).</summary>
    public sealed record Archetype
    {
        public required string Code { get; init; }           // e.g., "a_gray_1"
        public required Color Color { get; init; }           // Gray/Red/Green/...
        public required int Level { get; init; }             // 1..N
        public required int Hp { get; init; }                // base HP
        public required decimal DamageMultiplier { get; init; } // 1.00..1.30 typical
        public string Description { get; init; } = string.Empty;
    }
