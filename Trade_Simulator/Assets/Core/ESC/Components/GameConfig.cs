using Unity.Collections;
using Unity.Entities;

// =============================================
// КОНФИГУРАЦИЯ ИГРЫ (СИНГЛТОН)
// =============================================

public struct GameConfig : IComponentData
{
    // === СТАРТОВЫЕ НАСТРОЙКИ ===
    public int StartGold;              // Стартовое золото
    public int StartFood;              // Стартовый провиант
    public int StartGuards;            // Стартовая охрана

    // === НАСТРОЙКИ БАЛАНСА ===
    public float FoodConsumptionRate;  // Потребление пищи в день
    public float BaseMovementSpeed;    // Базовая скорость движения
    public float BaseWearRate;         // Базовая скорость износа

    // === НАСТРОЙКИ КАРТЫ ===
    public int MapWidth;               // Ширина карты
    public int MapHeight;              // Высота карты  
    public float WorldScale;           // Масштаб мира

    // === ЭКОНОМИЧЕСКИЕ НАСТРОЙКИ ===
    public float BasePriceMultiplier;  // Базовый множитель цен
    public float SupplyDemandImpact;   // Влияние спроса/предложения
    public float InflationRate;        // Скорость инфляции

    // === НАСТРОЙКИ СОБЫТИЙ ===
    public float EventChance;          // Шанс события в день
    public float CombatDifficulty;     // Сложность боев
    public float WeatherImpact;        // Влияние погоды

    // === НАСТРОЙКИ ПРОГРЕССА ===
    public int ExpPerLevel;            // Опыта для уровня
    public float LevelBonusMultiplier; // Множитель бонусов за уровень
}

// =============================================
// КОМПОНЕНТЫ СОБЫТИЙ И ПРОГРЕССА
// =============================================

// Игровое событие
public struct GameEvent : IComponentData
{
    public EventType Type;             // Тип события
    public float Severity;             // Серьезность (0.0 - 1.0)
    public float Duration;             // Длительность
    public FixedString128Bytes Description; // Описание
    public bool Processed;             // Обработано ли
    public Entity TargetEntity;        // Целевая сущность
}

// Прогресс игрока
public struct PlayerProgress : IComponentData
{
    public int Level;                  // Уровень игрока
    public int Experience;             // Текущий опыт
    public float TotalDistanceTraveled; // Пройденная дистанция
    public int TotalGoldEarned;        // Всего заработано золота
    public int TotalTradesCompleted;   // Завершенных сделок

    // Достижения
    public bool Achievement_FirstThousand;
    public bool Achievement_Explorer;
    public bool Achievement_MasterTrader;
    public bool Achievement_CaravanKing;
}

// Результат боя
public struct CombatResult : IComponentData
{
    public bool Victory;               // Победа ли игрока
    public int PlayerLosses;           // Потери игрока
    public int EnemyLosses;            // Потери врага
    public int GoldLost;               // Потеряно золота
    public int FoodLost;               // Потеряно провианта
    public float MoraleChange;         // Изменение морали
}

// Перечисления для событий и прогресса
public enum EventType
{
    // Негативные события
    WagonBreakdown,    // Поломка повозки
    BanditAttack,      // Нападение бандитов
    WeatherStorm,      // Погодный шторм
    RoadBlock,         // Заблокированная дорога
    Sickness,          // Болезнь в отряде

    // Позитивные события  
    TradeOpportunity,  // Торговая возможность
    LuckyFind,         // Удачная находка
    GoodWeather,       // Благоприятная погода
    NewRecruits,       // Новые рекруты

    // Нейтральные события
    CityEvent,         // Событие в городе
    TravelEncounter    // Встреча в пути
}

public enum CombatOutcome
{
    DecisiveVictory,   // Решительная победа
    Victory,           // Победа
    PyrrhicVictory,    // Пиррова победа  
    Stalemate,         // Ничья
    Defeat,            // Поражение
    Rout               // Разгром
}