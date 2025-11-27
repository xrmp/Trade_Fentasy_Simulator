using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class PlayerAuthoring : MonoBehaviour
{
    [Header("Стартовые настройки игрока")]
    [Tooltip("Начальная позиция игрока на карте")]
    public Vector2 startGridPosition = new Vector2(10, 10);

    [Tooltip("Начальная грузоподъемность обоза")]
    public int startCapacity = 1000;

    [Tooltip("Начальная мораль отряда (0.0 - 1.0)")]
    [Range(0.1f, 1.0f)]
    public float startMorale = 1.0f;

    [Header("Стартовая повозка")]
    [Tooltip("Тип стартовой повозки")]
    public WagonType startWagonType = WagonType.BasicCart;

    [Tooltip("Здоровье стартовой повозки")]
    public int startWagonHealth = 100;

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Debug.Log("🎮 PlayerAuthoring: Выполняется Baker...");

            // Этот Baker создает только данные для PlayerCreationSystem
            // Сам игрок создается в системе для контроля порядка инициализации

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Добавляем временный компонент с настройками игрока
            AddComponent(entity, new PlayerSettings
            {
                StartGridPosition = new int2((int)authoring.startGridPosition.x, (int)authoring.startGridPosition.y),
                StartCapacity = authoring.startCapacity,
                StartMorale = authoring.startMorale,
                StartWagonType = authoring.startWagonType,
                StartWagonHealth = authoring.startWagonHealth
            });

            Debug.Log($"✅ Настройки игрока сохранены: позиция {authoring.startGridPosition}");
        }
    }
}

// Временный компонент для хранения настроек игрока
public struct PlayerSettings : IComponentData
{
    public int2 StartGridPosition;
    public int StartCapacity;
    public float StartMorale;
    public WagonType StartWagonType;
    public int StartWagonHealth;
}