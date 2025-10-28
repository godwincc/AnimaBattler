#nullable enable
namespace AnimaBattler.Core.Anima;

  public enum Color
    {
        Gray,
        Red,
        Green,
        Blue,
        Yellow,
        Purple
    }


public enum PartSlot { A, B, C, D, E, F }
public enum PartType   { Attack, Heal, Buff, Debuff, Passive, DeathTrigger }

public enum Target
{
    Self,
    Ally,
    AllyLowestHp,
    AllAllies,
    Enemy,
    EnemyFront,
    EnemyLowestHp,
    FrontEnemies,
    RowEnemies,
    AnyEnemy
}