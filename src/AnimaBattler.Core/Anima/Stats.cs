#nullable enable
namespace AnimaBattler.Core.Anima;

/// <summary>Base stat line. Keep tiny for now.</summary>
public sealed class Stats
{
    public int MaxHp  { get; init; }
    public int Speed  { get; set; }

    public Stats(int maxHp, int speed) => (MaxHp, Speed) = (maxHp, speed);
}
