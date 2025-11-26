using Unity.Entities;
using Unity.Mathematics;

public struct CombatStats : IComponentData
{
    public int AttackPower;
    public int DefensePower;
    public float AttackSpeed;
    public int Health;
    public int MaxHealth;
}

public struct GuardData : IComponentData
{
    public Entity Owner;
    public GuardType Type;
    public int Level;
    public int Experience;
    public int Salary;
    public float Morale;
    public bool IsInCombat;
}

public struct BanditEncounter : IComponentData
{
    public int BanditCount;
    public int BanditPower;
    public float SurpriseFactor;
    public bool Resolved;
}

public struct CombatResult : IComponentData
{
    public bool Victory;
    public int PlayerLosses;
    public int BanditLosses;
    public int GoldLost;
    public int FoodLost;
    public float MoraleChange;
}

public enum GuardType
{
    Militia,
    Mercenary,
    EliteGuard
}