using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;

public class WagonUIManager : MonoBehaviour
{
    [Header("Панель управления повозками")]
    public GameObject wagonsPanel;
    public Transform wagonsContent;
    public GameObject wagonUIPrefab;

    [Header("Информация о повозке")]
    public TMP_Text selectedWagonName;
    public TMP_Text selectedWagonHealth;
    public TMP_Text selectedWagonCapacity;
    public TMP_Text selectedWagonStatus;

    [Header("Кнопки действий")]
    public Button repairButton;
    public Button replaceButton;

    [Header("Покупка новых повозок")]
    public GameObject purchasePanel;
    public Button buyBasicCartButton;
    public Button buyTradeWagonButton;
    public Button buyHeavyWagonButton;
    public Button buyLuxuryCoachButton;

    private Entity _selectedWagon = Entity.Null;
    private Dictionary<Entity, GameObject> _wagonUIItems = new Dictionary<Entity, GameObject>();

    void Start()
    {
        repairButton.onClick.AddListener(RepairSelectedWagon);
        replaceButton.onClick.AddListener(ReplaceSelectedWagon);

        buyBasicCartButton.onClick.AddListener(() => BuyWagon(WagonType.BasicCart));
        buyTradeWagonButton.onClick.AddListener(() => BuyWagon(WagonType.TradeWagon));
        buyHeavyWagonButton.onClick.AddListener(() => BuyWagon(WagonType.HeavyWagon));
        buyLuxuryCoachButton.onClick.AddListener(() => BuyWagon(WagonType.LuxuryCoach));

        wagonsPanel.SetActive(false);
        purchasePanel.SetActive(false);
    }

    void Update()
    {
        if (wagonsPanel.activeInHierarchy)
        {
            UpdateWagonsUI();
        }
    }

    public void OpenWagonsManager()
    {
        wagonsPanel.SetActive(true);
        UpdateWagonsUI();
    }

    public void CloseWagonsManager()
    {
        wagonsPanel.SetActive(false);
        purchasePanel.SetActive(false);
        ClearWagonsUI();
    }

    public void ShowPurchasePanel()
    {
        purchasePanel.SetActive(true);
    }

    public void HidePurchasePanel()
    {
        purchasePanel.SetActive(false);
    }

    private void UpdateWagonsUI()
    {
        ClearWagonsUI();

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var wagonQuery = entityManager.CreateEntityQuery(typeof(Wagon));
        var wagons = wagonQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var wagonEntity in wagons)
        {
            var wagon = entityManager.GetComponentData<Wagon>(wagonEntity);
            AddWagonUI(wagonEntity, wagon, entityManager);
        }

        wagons.Dispose();

        // Обновляем информацию о выбранной повозке
        UpdateSelectedWagonInfo();
    }

    private void AddWagonUI(Entity wagonEntity, Wagon wagon, EntityManager entityManager)
    {
        var wagonUI = Instantiate(wagonUIPrefab, wagonsContent);
        var texts = wagonUI.GetComponentsInChildren<TMP_Text>();

        texts[0].text = $"Повозка ({wagon.WagonType})";
        texts[1].text = $"Прочность: {wagon.Health}/{wagon.MaxHealth}";
        texts[2].text = $"Груз: {wagon.CurrentLoad}/{wagon.LoadCapacity}";
        texts[3].text = wagon.IsBroken ? "СЛОМАНА" : "Исправна";
        texts[3].color = wagon.IsBroken ? Color.red : Color.green;

        var button = wagonUI.GetComponent<Button>();
        button.onClick.AddListener(() => SelectWagon(wagonEntity));

        _wagonUIItems[wagonEntity] = wagonUI;

        // Выбираем первую повозку если ничего не выбрано
        if (_selectedWagon == Entity.Null)
        {
            SelectWagon(wagonEntity);
        }
    }

    private void SelectWagon(Entity wagonEntity)
    {
        _selectedWagon = wagonEntity;
        UpdateSelectedWagonInfo();
    }

    private void UpdateSelectedWagonInfo()
    {
        if (_selectedWagon == Entity.Null) return;

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (!entityManager.Exists(_selectedWagon)) return;

        var wagon = entityManager.GetComponentData<Wagon>(_selectedWagon);

        selectedWagonName.text = $"Повозка ({wagon.WagonType})";
        selectedWagonHealth.text = $"Прочность: {wagon.Health}/{wagon.MaxHealth}";
        selectedWagonCapacity.text = $"Грузоподъемность: {wagon.LoadCapacity}";
        selectedWagonStatus.text = wagon.IsBroken ? "Статус: СЛОМАНА" : "Статус: Исправна";
        selectedWagonStatus.color = wagon.IsBroken ? Color.red : Color.green;

        // Обновляем доступность кнопок
        repairButton.interactable = wagon.IsBroken;
        replaceButton.interactable = true;
    }

    private void RepairSelectedWagon()
    {
        if (_selectedWagon == Entity.Null) return;

        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var repairEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(repairEntity, new WagonRepair
        {
            WagonEntity = _selectedWagon
        });

        Debug.Log("🔧 Запрошен ремонт повозки");
    }

    private void ReplaceSelectedWagon()
    {
        if (_selectedWagon == Entity.Null) return;

        // В реальной системе здесь будет логика замены повозки
        Debug.Log("🔄 Замена повозки");
    }

    private void BuyWagon(WagonType wagonType)
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var purchaseEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(purchaseEntity, new WagonPurchase
        {
            WagonType = wagonType
        });

        Debug.Log($"🛒 Запрос на покупку повозки: {wagonType}");
        HidePurchasePanel();
    }

    private void ClearWagonsUI()
    {
        foreach (var item in _wagonUIItems.Values)
        {
            Destroy(item);
        }
        _wagonUIItems.Clear();
    }
}