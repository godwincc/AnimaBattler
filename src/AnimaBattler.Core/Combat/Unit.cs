#nullable enable
namespace AnimaBattler.Core.Combat;

public abstract class Unit
{
    public required string Id { get; init; }
    public int CurrentHp { get; protected set; }
    public int CurrentShield { get; protected set; }
    public int TurnCounter { get; protected set; }

    public abstract int BaseHp { get; }
    public abstract decimal DamageMultiplier { get; }

    public virtual void AddShield(int amount)
    {
        if (amount > 0)
            CurrentShield += amount;
    }

    public virtual int ReceiveDamage(int damage)
    {
        if (damage <= 0) return 0;

        var remaining = damage;

        if (CurrentShield > 0)
        {
            var absorbed = Math.Min(CurrentShield, remaining);
            CurrentShield -= absorbed;
            remaining -= absorbed;
        }

        if (remaining > 0)
        {
            var oldHp = CurrentHp;
            CurrentHp = Math.Max(0, CurrentHp - remaining);
            return oldHp - CurrentHp;
        }

        return 0;
    }

    public virtual void NextTurn() => TurnCounter++;

    public bool IsDefeated => CurrentHp <= 0;
}
