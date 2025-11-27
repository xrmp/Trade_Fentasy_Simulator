using UnityEngine;
using Unity.Entities;

public class GameDataAuthoring : MonoBehaviour
{
    [Header("Стартовые ресурсы")]
    [Tooltip("Начальное количество золота у игрока")]
    public int startGold = 1000;

    [Tooltip("Начальное количество провианта")]
    public int startFood = 100;

    [Tooltip("Начальное количество охраны")]
    public int startGuards = 5;

    [Header("Настройки баланса")]
    [Tooltip("Потребление провианта в день на одного человека")]
    public float foodConsumptionRate = 2f;

    [Tooltip("Базовая скорость движения обоза")]
    public float baseMovementSpeed = 5f;

    [Tooltip("Базовая скорость износа повозок")]
    public float baseWearRate = 0.1f;

    [Header("Настройки карты")]
    [Tooltip("Ширина игровой карты в тайлах")]
    public int mapWidth = 100;

    [Tooltip("Высота игровой карты в тайлах")]
    public int mapHeight = 100;

    [Tooltip("Масштаб мира (размер тайла)")]
    public float worldScale = 10f;

    [Header("Экономические настройки")]
    [Tooltip("Базовый множитель цен")]
    public float basePriceMultiplier = 1.0f;

    [Tooltip("Влияние спроса/предложения на цены")]
    public float supplyDemandImpact = 0.5f;

    [Tooltip("Скорость инфляции цен")]
    public float inflationRate = 0.01f;

    [Header("Настройки событий")]
    [Tooltip("Базовый шанс события в день")]
    public float eventChance = 0.25f;

    [Tooltip("Сложность боев (множитель силы врагов)")]
    public float combatDifficulty = 1.0f;

    [Tooltip("Влияние погоды на скорость движения")]
    public float weatherImpact = 0.3f;

    [Header("Настройки прогресса")]
    [Tooltip("Опыта необходимого для повышения уровня")]
    public int expPerLevel = 100;

    [Tooltip("Множитель бонусов за уровень")]
    public float levelBonusMultiplier = 1.1f;

    class Baker : Baker<GameDataAuthoring>
    {
        public override void Bake(GameDataAuthoring authoring)
        {
            Debug.Log("🎯 GameDataAuthoring: Выполняется Baker...");

            var entity = GetEntity(TransformUsageFlags.None);

            // Создаем GameConfig как синглтон
            var gameConfig = new GameConfig
            {
                // Стартовые настройки
                StartGold = authoring.startGold,
                StartFood = authoring.startFood,
                StartGuards = authoring.startGuards,

                // Настройки баланса
                FoodConsumptionRate = authoring.foodConsumptionRate,
                BaseMovementSpeed = authoring.baseMovementSpeed,
                BaseWearRate = authoring.baseWearRate,

                // Настройки карты
                MapWidth = authoring.mapWidth,
                MapHeight = authoring.mapHeight,
                WorldScale = authoring.worldScale,

                // Экономические настройки
                BasePriceMultiplier = authoring.basePriceMultiplier,
                SupplyDemandImpact = authoring.supplyDemandImpact,
                InflationRate = authoring.inflationRate,

                // Настройки событий
                EventChance = authoring.eventChance,
                CombatDifficulty = authoring.combatDifficulty,
                WeatherImpact = authoring.weatherImpact,

                // Настройки прогресса
                ExpPerLevel = authoring.expPerLevel,
                LevelBonusMultiplier = authoring.levelBonusMultiplier
            };

            AddComponent(entity, gameConfig);

            Debug.Log($"✅ GameConfig создан: {authoring.startGold} золота, {authoring.startFood} еды, {authoring.startGuards} охраны");
        }
    }
}