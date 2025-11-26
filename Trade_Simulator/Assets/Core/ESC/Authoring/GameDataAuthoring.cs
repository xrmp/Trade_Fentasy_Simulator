using UnityEngine;
using Unity.Entities;

public class GameDataAuthoring : MonoBehaviour
{
    [Header("Стартовые ресурсы")]
    public int startGold = 1000;
    public int startFood = 100;
    public int startGuards = 5;

    [Header("Настройки баланса")]
    public float foodConsumptionRate = 2f;
    public float baseMovementSpeed = 5f;

    class Baker : Baker<GameDataAuthoring>
    {
        public override void Bake(GameDataAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new GameConfig
            {
                StartGold = authoring.startGold,
                StartFood = authoring.startFood,
                StartGuards = authoring.startGuards,
                FoodConsumptionRate = authoring.foodConsumptionRate,
                BaseMovementSpeed = authoring.baseMovementSpeed
            });
        }
    }
}

// GameConfig ДОЛЖЕН БЫТЬ ТОЛЬКО ЗДЕСЬ!
public struct GameConfig : IComponentData
{
    public int StartGold;
    public int StartFood;
    public int StartGuards;
    public float FoodConsumptionRate;
    public float BaseMovementSpeed;
}