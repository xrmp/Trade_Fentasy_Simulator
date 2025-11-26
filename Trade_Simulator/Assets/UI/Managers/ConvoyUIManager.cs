using UnityEngine;
using TMPro;
using Unity.Entities;

public class ConvoyUIManager : MonoBehaviour
{
    public TMP_Text goldText;
    public TMP_Text foodText;
    public TMP_Text guardsText;
    
    void Update()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;
        
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag), typeof(ConvoyResources));
        
        if (!playerQuery.IsEmpty)
        {
            var resources = playerQuery.GetSingleton<ConvoyResources>();
            goldText.text = $"Gold: {resources.Gold}";
            foodText.text = $"Food: {resources.Food}";
            guardsText.text = $"Guards: {resources.Guards}";
        }
    }
}