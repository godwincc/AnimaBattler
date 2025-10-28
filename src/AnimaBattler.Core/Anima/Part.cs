#nullable enable
namespace AnimaBattler.Core.Anima;

 /// <summary>Represents a modular component that grants a skill or passive.</summary>
    public sealed record Part
    {
        public required string Id { get; init; }            // e.g., "GRAY_A1_STONE_CREST"
        public required PartSlot Slot { get; init; }        // A..F
        public required string Name { get; init; }          // display name
        public required string Text { get; init; }          // rules text (UI/tooltip)
        public bool IsDeathTrigger { get; init; }           // only one death-trigger may activate
    }