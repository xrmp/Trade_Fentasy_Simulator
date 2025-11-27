using UnityEngine;
using Unity.Entities;

public class GoodsAuthoring : MonoBehaviour
{
    [Header("Настройки товара")]
    [Tooltip("Название товара")]
    public string goodName = "Товар";

    [Tooltip("Категория товара")]
    public GoodCategory category = GoodCategory.RawMaterials;

    [Tooltip("Вес за единицу товара")]
    public int weightPerUnit = 1;

    [Tooltip("Базовая стоимость товара")]
    public int baseValue = 10;

    [Tooltip("Прибыль за км перевозки")]
    public float profitPerKm = 0.1f;

    [Tooltip("Скорость порчи товара (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float decayRate = 0.1f;

    class Baker : Baker<GoodsAuthoring>
    {
        public override void Bake(GoodsAuthoring authoring)
        {
            Debug.Log($"📦 GoodsAuthoring: Создаем товар {authoring.goodName}...");

            var entity = GetEntity(TransformUsageFlags.None);

            // Создаем компонент товара
            AddComponent(entity, new GoodData
            {
                Name = authoring.goodName,
                WeightPerUnit = authoring.weightPerUnit,
                BaseValue = authoring.baseValue,
                Category = authoring.category,
                ProfitPerKm = authoring.profitPerKm,
                DecayRate = authoring.decayRate
            });

            Debug.Log($"✅ Товар создан: {authoring.goodName}, цена: {authoring.baseValue}, вес: {authoring.weightPerUnit}");
        }
    }
}