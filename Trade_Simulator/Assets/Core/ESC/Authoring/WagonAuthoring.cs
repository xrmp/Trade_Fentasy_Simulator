using UnityEngine;
using Unity.Entities;

public class WagonAuthoring : MonoBehaviour
{
    [Header("Настройки повозки")]
    [Tooltip("Тип повозки")]
    public WagonType wagonType = WagonType.BasicCart;

    [Tooltip("Максимальное здоровье повозки")]
    public int maxHealth = 100;

    [Tooltip("Грузоподъемность повозки")]
    public int loadCapacity = 500;

    [Tooltip("Модификатор скорости повозки")]
    [Range(0.5f, 1.5f)]
    public float speedModifier = 1.0f;

    [Tooltip("Скорость износа повозки")]
    [Range(0.05f, 0.2f)]
    public float wearRate = 0.1f;

    [Header("Начальное состояние")]
    [Tooltip("Начальное здоровье повозки")]
    public int startHealth = 100;

    [Tooltip("Начальная загрузка повозки")]
    public int startLoad = 0;

    [Tooltip("Сломана ли повозка изначально")]
    public bool startBroken = false;

    class Baker : Baker<WagonAuthoring>
    {
        public override void Bake(WagonAuthoring authoring)
        {
            Debug.Log($"🚛 WagonAuthoring: Создаем повозку {authoring.wagonType}...");

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Добавляем тэг повозки
            AddComponent<WagonTag>(entity);

            // Создаем компонент повозки
            AddComponent(entity, new Wagon
            {
                Owner = Entity.Null, // Будет установлен при присоединении к игроку
                Health = authoring.startHealth,
                MaxHealth = authoring.maxHealth,
                LoadCapacity = authoring.loadCapacity,
                CurrentLoad = authoring.startLoad,
                SpeedModifier = authoring.speedModifier,
                WearRate = authoring.wearRate,
                Type = authoring.wagonType,
                IsBroken = authoring.startBroken
            });

            Debug.Log($"✅ Повозка создана: {authoring.wagonType}, здоровье: {authoring.startHealth}/{authoring.maxHealth}");
        }
    }
}