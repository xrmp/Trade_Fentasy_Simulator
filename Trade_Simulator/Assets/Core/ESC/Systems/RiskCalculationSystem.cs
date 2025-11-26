using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct RiskCalculationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Расчет рисков для текущей позиции игрока
        foreach (var (position, resources) in
                 SystemAPI.Query<RefRO<MapPosition>, RefRO<ConvoyResources>>())
        {
            var riskLevel = CalculateCurrentRisk(position.ValueRO, resources.ValueRO);

            // Можно сохранить уровень риска для использования в других системах
            // Например, для влияния на частоту событий
        }
    }

    private float CalculateCurrentRisk(MapPosition position, ConvoyResources resources)
    {
        var baseRisk = GetTerrainRisk(position.CurrentTerrainType);
        var guardModifier = math.max(0.1f, 1.0f - (resources.Guards * 0.05f));
        var moraleModifier = math.max(0.5f, resources.Morale);

        return baseRisk * guardModifier * moraleModifier;
    }

    private float GetTerrainRisk(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Forest => 0.7f,
            TerrainType.Mountains => 0.8f,
            TerrainType.Desert => 0.6f,
            TerrainType.River => 0.5f,
            TerrainType.Road => 0.3f,
            TerrainType.Plains => 0.4f,
            _ => 0.5f
        };
    }
}

// Компонент для хранения информации о рисках
public struct RiskAssessment : IComponentData
{
    public float CurrentRisk;
    public float BanditRisk;
    public float EnvironmentalRisk;
    public float TravelRisk;
}