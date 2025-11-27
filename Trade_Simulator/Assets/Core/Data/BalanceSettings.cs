using UnityEngine;
using Unity.Entities;
using System;

namespace Core.Data
{

    [CreateAssetMenu(fileName = "BalanceSettings", menuName = "Game/Balance Settings")]
    public class BalanceSettings : ScriptableObject
    {
        [Header("⚡ ОСНОВНЫЕ НАСТРОЙКИ")]
        [Tooltip("Версия баланса для контроля совместимости")]
        public string balanceVersion = "1.0.0";

        [Header("💰 ЭКОНОМИКА И ТОРГОВЛЯ")]
        [SerializeField] private EconomicSettings economicSettings;

        [Header("🚛 СИСТЕМА ОБОЗА")]
        [SerializeField] private ConvoySettings convoySettings;

        [Header("🗺️ КАРТА И ПУТЕШЕСТВИЯ")]
        [SerializeField] private MapTravelSettings mapTravelSettings;

        [Header("⚔️ БОЕВАЯ СИСТЕМА")]
        [SerializeField] private CombatSettings combatSettings;

        [Header("🎯 СИСТЕМА СОБЫТИЙ")]
        [SerializeField] private EventSettings eventSettings;

        [Header("📈 СИСТЕМА ПРОГРЕССА")]
        [SerializeField] private ProgressionSettings progressionSettings;

        // Свойства для доступа к настройкам
        public EconomicSettings Economic => economicSettings;
        public ConvoySettings Convoy => convoySettings;
        public MapTravelSettings MapTravel => mapTravelSettings;
        public CombatSettings Combat => combatSettings;
        public EventSettings Events => eventSettings;
        public ProgressionSettings Progression => progressionSettings;


        public bool ValidateSettings()
        {
            bool isValid = true;

            if (economicSettings == null)
            {
                Debug.LogError("❌ BalanceSettings: EconomicSettings не назначены!");
                isValid = false;
            }

            if (convoySettings == null)
            {
                Debug.LogError("❌ BalanceSettings: ConvoySettings не назначены!");
                isValid = false;
            }

            // Проверка числовых значений на валидность
            if (economicSettings != null)
            {
                if (economicSettings.basePriceMultiplier <= 0)
                {
                    Debug.LogError("❌ BalanceSettings: basePriceMultiplier должен быть больше 0!");
                    isValid = false;
                }
            }

            if (isValid)
            {
                Debug.Log("✅ BalanceSettings: Все настройки валидны");
            }

            return isValid;
        }


        public void ResetToDefaults()
        {
            economicSettings = new EconomicSettings();
            convoySettings = new ConvoySettings();
            mapTravelSettings = new MapTravelSettings();
            combatSettings = new CombatSettings();
            eventSettings = new EventSettings();
            progressionSettings = new ProgressionSettings();

            Debug.Log("🔄 BalanceSettings: Настройки сброшены к значениям по умолчанию");
        }
    }

    // =============================================
    // КЛАССЫ НАСТРОЕК
    // =============================================

    [Serializable]
    public class EconomicSettings
    {
        [Header("Общие экономические настройки")]
        [Tooltip("Базовый множитель цен")]
        [Range(0.1f, 5.0f)]
        public float basePriceMultiplier = 1.0f;

        [Tooltip("Влияние спроса/предложения на цены")]
        [Range(0.1f, 2.0f)]
        public float supplyDemandImpact = 0.5f;

        [Tooltip("Скорость инфляции цен (в день)")]
        [Range(0.0f, 0.1f)]
        public float inflationRate = 0.01f;

        [Tooltip("Скорость восстановления цен после торговли")]
        [Range(0.1f, 1.0f)]
        public float priceRecoveryRate = 0.3f;

        [Header("Региональные модификаторы цен")]
        [Tooltip("Модификатор цен в аграрных регионах")]
        [Range(0.5f, 2.0f)]
        public float agriculturalPriceModifier = 0.8f;

        [Tooltip("Модификатор цен в индустриальных регионах")]
        [Range(0.5f, 2.0f)]
        public float industrialPriceModifier = 1.2f;

        [Tooltip("Модификатор цен в торговых узлах")]
        [Range(0.5f, 2.0f)]
        public float tradeHubPriceModifier = 1.0f;

        [Tooltip("Модификатор цен в горнодобывающих регионах")]
        [Range(0.5f, 2.0f)]
        public float miningPriceModifier = 1.1f;

        [Header("Налоги и комиссии")]
        [Tooltip("Налог на продажу товаров (%)")]
        [Range(0f, 0.3f)]
        public float salesTax = 0.05f;

        [Tooltip("Комиссия за использование рынка (%)")]
        [Range(0f, 0.2f)]
        public float marketFee = 0.02f;
    }

    [Serializable]
    public class ConvoySettings
    {
        [Header("Настройки движения")]
        [Tooltip("Базовая скорость движения обоза")]
        [Range(1f, 20f)]
        public float baseMovementSpeed = 5f;

        [Tooltip("Максимальная скорость обоза")]
        [Range(5f, 30f)]
        public float maxMovementSpeed = 15f;

        [Tooltip("Минимальная скорость обоза")]
        [Range(0.1f, 5f)]
        public float minMovementSpeed = 1f;

        [Header("Настройки грузоподъемности")]
        [Tooltip("Базовая грузоподъемность одной повозки")]
        [Range(100, 2000)]
        public int baseWagonCapacity = 500;

        [Tooltip("Максимальная грузоподъемность обоза")]
        [Range(1000, 10000)]
        public int maxConvoyCapacity = 5000;

        [Tooltip("Штраф к скорости за перегрузку (%)")]
        [Range(0f, 1f)]
        public float overloadSpeedPenalty = 0.5f;

        [Header("Настройки износа повозок")]
        [Tooltip("Базовая скорость износа повозок")]
        [Range(0.01f, 1.0f)]
        public float baseWearRate = 0.1f;

        [Tooltip("Множитель износа при перегрузке")]
        [Range(1f, 5f)]
        public float overloadWearMultiplier = 2.0f;

        [Tooltip("Стоимость ремонта за единицу здоровья")]
        [Range(0.1f, 5f)]
        public float repairCostPerHealth = 1.0f;

        [Header("Типы повозок")]
        public WagonTypeSettings[] wagonTypes = new WagonTypeSettings[]
        {
            new WagonTypeSettings { type = WagonType.BasicCart, capacity = 500, health = 100, speed = 1.0f, cost = 100 },
            new WagonTypeSettings { type = WagonType.TradeWagon, capacity = 800, health = 150, speed = 0.9f, cost = 200 },
            new WagonTypeSettings { type = WagonType.HeavyWagon, capacity = 1200, health = 200, speed = 0.7f, cost = 300 },
            new WagonTypeSettings { type = WagonType.LuxuryCoach, capacity = 600, health = 120, speed = 1.1f, cost = 500 }
        };
    }

    [Serializable]
    public class MapTravelSettings
    {
        [Header("Модификаторы местности")]
        [Tooltip("Скорость движения по равнинам")]
        [Range(0.1f, 2.0f)]
        public float plainsSpeedModifier = 1.0f;

        [Tooltip("Скорость движения по лесам")]
        [Range(0.1f, 2.0f)]
        public float forestSpeedModifier = 0.7f;

        [Tooltip("Скорость движения по горам")]
        [Range(0.1f, 2.0f)]
        public float mountainSpeedModifier = 0.5f;

        [Tooltip("Скорость движения по дорогам")]
        [Range(0.1f, 2.0f)]
        public float roadSpeedModifier = 1.2f;

        [Tooltip("Скорость движения по пустыне")]
        [Range(0.1f, 2.0f)]
        public float desertSpeedModifier = 0.6f;

        [Tooltip("Скорость движения через реки")]
        [Range(0.1f, 2.0f)]
        public float riverSpeedModifier = 0.4f;

        [Header("Износ повозок на местности")]
        [Tooltip("Износ на равнинах")]
        [Range(0.1f, 3.0f)]
        public float plainsWearMultiplier = 1.0f;

        [Tooltip("Износ в лесах")]
        [Range(0.1f, 3.0f)]
        public float forestWearMultiplier = 1.3f;

        [Tooltip("Износ в горах")]
        [Range(0.1f, 3.0f)]
        public float mountainWearMultiplier = 2.0f;

        [Tooltip("Износ на дорогах")]
        [Range(0.1f, 3.0f)]
        public float roadWearMultiplier = 0.7f;

        [Header("Опасность местности")]
        [Tooltip("Уровень опасности в лесах")]
        [Range(0.0f, 1.0f)]
        public float forestDangerLevel = 0.7f;

        [Tooltip("Уровень опасности в горах")]
        [Range(0.0f, 1.0f)]
        public float mountainDangerLevel = 0.8f;

        [Tooltip("Уровень опасности в пустыне")]
        [Range(0.0f, 1.0f)]
        public float desertDangerLevel = 0.6f;

        [Tooltip("Уровень опасности на дорогах")]
        [Range(0.0f, 1.0f)]
        public float roadDangerLevel = 0.3f;
    }

    [Serializable]
    public class CombatSettings
    {
        [Header("Баланс боевой системы")]
        [Tooltip("Базовая сила одного охранника")]
        [Range(5, 50)]
        public int baseGuardPower = 10;

        [Tooltip("Множитель силы от морали")]
        [Range(0.5f, 2.0f)]
        public float moralePowerMultiplier = 1.0f;

        [Tooltip("Сложность боев (множитель силы врагов)")]
        [Range(0.5f, 3.0f)]
        public float combatDifficulty = 1.0f;

        [Tooltip("Фактор случайности в боях")]
        [Range(0.1f, 0.5f)]
        public float combatRandomFactor = 0.2f;

        [Header("Потери и последствия")]
        [Tooltip("Базовый шанс потери охранника в бою")]
        [Range(0.0f, 1.0f)]
        public float baseGuardLossChance = 0.1f;

        [Tooltip("Шанс повреждения повозки в бою")]
        [Range(0.0f, 1.0f)]
        public float wagonDamageChance = 0.3f;

        [Tooltip("Шанс потери товаров в бою")]
        [Range(0.0f, 1.0f)]
        public float goodsLossChance = 0.2f;

        [Tooltip("Изменение морали после боя (победа)")]
        [Range(-0.5f, 0.5f)]
        public float moraleChangeVictory = 0.1f;

        [Tooltip("Изменение морали после боя (поражение)")]
        [Range(-0.5f, 0.5f)]
        public float moraleChangeDefeat = -0.3f;

        [Header("Типы охраны")]
        public GuardTypeSettings[] guardTypes = new GuardTypeSettings[]
        {
            new GuardTypeSettings { type = GuardType.Militia, power = 8, salary = 2, morale = 0.8f, cost = 50 },
            new GuardTypeSettings { type = GuardType.Mercenary, power = 12, salary = 5, morale = 1.0f, cost = 100 },
            new GuardTypeSettings { type = GuardType.EliteGuard, power = 20, salary = 10, morale = 1.2f, cost = 200 }
        };
    }

    [Serializable]
    public class EventSettings
    {
        [Header("Вероятности событий")]
        [Tooltip("Базовый шанс события в день")]
        [Range(0.0f, 1.0f)]
        public float baseEventChance = 0.25f;

        [Tooltip("Шанс события в лесах")]
        [Range(0.0f, 1.0f)]
        public float forestEventChance = 0.4f;

        [Tooltip("Шанс события в горах")]
        [Range(0.0f, 1.0f)]
        public float mountainEventChance = 0.5f;

        [Tooltip("Шанс события на дорогах")]
        [Range(0.0f, 1.0f)]
        public float roadEventChance = 0.1f;

        [Header("Настройки конкретных событий")]
        [Tooltip("Шанс нападения бандитов")]
        [Range(0.0f, 1.0f)]
        public float banditAttackChance = 0.3f;

        [Tooltip("Шанс поломки повозки")]
        [Range(0.0f, 1.0f)]
        public float wagonBreakdownChance = 0.2f;

        [Tooltip("Шанс благоприятной погоды")]
        [Range(0.0f, 1.0f)]
        public float goodWeatherChance = 0.15f;

        [Tooltip("Шанс торговой возможности")]
        [Range(0.0f, 1.0f)]
        public float tradeOpportunityChance = 0.1f;

        [Header("Влияние погоды")]
        [Tooltip("Влияние погоды на скорость движения")]
        [Range(0.1f, 0.5f)]
        public float weatherImpact = 0.3f;

        [Tooltip("Длительность погодных эффектов")]
        [Range(10f, 60f)]
        public float weatherDuration = 30f;
    }

    [Serializable]
    public class ProgressionSettings
    {
        [Header("Система уровней")]
        [Tooltip("Опыта необходимого для повышения уровня")]
        [Range(50, 500)]
        public int expPerLevel = 100;

        [Tooltip("Множитель опыта для следующих уровней")]
        [Range(1.0f, 2.0f)]
        public float levelExpMultiplier = 1.2f;

        [Tooltip("Множитель бонусов за уровень")]
        [Range(1.0f, 1.5f)]
        public float levelBonusMultiplier = 1.1f;

        [Header("Бонусы за уровни")]
        [Tooltip("Бонус к грузоподъемности за уровень")]
        [Range(0f, 0.2f)]
        public float capacityBonusPerLevel = 0.05f;

        [Tooltip("Бонус к скорости за уровень")]
        [Range(0f, 0.1f)]
        public float speedBonusPerLevel = 0.02f;

        [Tooltip("Бонус к торговым ценам за уровень")]
        [Range(0f, 0.05f)]
        public float tradeBonusPerLevel = 0.01f;

        [Header("Достижения и репутация")]
        [Tooltip("Опыт за пройденный километр")]
        [Range(0.1f, 5f)]
        public float expPerKilometer = 1.0f;

        [Tooltip("Опыт за завершенную торговую сделку")]
        [Range(1f, 20f)]
        public float expPerTrade = 5f;

        [Tooltip("Опыт за победу в бою")]
        [Range(5f, 50f)]
        public float expPerCombatVictory = 10f;
    }

    // =============================================
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ
    // =============================================

    [Serializable]
    public class WagonTypeSettings
    {
        public WagonType type;
        [Range(100, 2000)] public int capacity;
        [Range(50, 500)] public int health;
        [Range(0.5f, 2.0f)] public float speed;
        [Range(50, 1000)] public int cost;
    }

    [Serializable]
    public class GuardTypeSettings
    {
        public GuardType type;
        [Range(5, 50)] public int power;
        [Range(1, 20)] public int salary;
        [Range(0.5f, 2.0f)] public float morale;
        [Range(25, 500)] public int cost;
    }

    // =============================================
    // ПЕРЕЧИСЛЕНИЯ
    // =============================================

    public enum WagonType
    {
        BasicCart,     // Базовая повозка
        TradeWagon,    // Торговая повозка  
        HeavyWagon,    // Тяжелая повозка
        LuxuryCoach    // Роскошная карета
    }

    public enum GuardType
    {
        Militia,       // Ополчение (дешево, слабо)
        Mercenary,     // Наемники (среднее)
        EliteGuard     // Элитная охрана (дорого, сильно)
    }
}