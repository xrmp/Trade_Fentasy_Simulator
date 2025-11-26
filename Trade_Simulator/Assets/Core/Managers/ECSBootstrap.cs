using UnityEngine;
using Unity.Entities;

public class ECSBootstrap : MonoBehaviour
{
    public static ECSBootstrap Instance;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            DefaultWorldInitialization.Initialize("Default World");
            Debug.Log("✅ ECS World инициализирован");
        }
    }
}