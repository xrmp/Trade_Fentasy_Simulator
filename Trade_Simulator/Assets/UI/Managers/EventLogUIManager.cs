using UnityEngine;
using TMPro;
using Unity.Entities;
using System.Collections.Generic;
using System.Collections;

public class EventLogUIManager : MonoBehaviour
{
    [Header("Журнал событий")]
    public GameObject logPanel;
    public Transform logContent;
    public GameObject logEntryPrefab;
    public TMP_Text newEventsCounter;

    [Header("Настройки")]
    public int maxEntries = 20;
    public float entryLifetime = 10f;

    private Queue<GameObject> _logEntries = new Queue<GameObject>();
    private int _newEventsCount = 0;
    private bool _needsRefresh = false;

    void Start()
    {
        logPanel.SetActive(false);
        UpdateNewEventsCounter();
    }

    void Update()
    {
        if (_needsRefresh)
        {
            RefreshEventLog();
            _needsRefresh = false;
        }
    }

    public void ToggleLogPanel()
    {
        logPanel.SetActive(!logPanel.activeInHierarchy);

        if (logPanel.activeInHierarchy)
        {
            _newEventsCount = 0;
            UpdateNewEventsCounter();
            RefreshEventLog();
        }
    }

    public void AddLogEntry(string message, EventType eventType)
    {
        if (logContent == null || logEntryPrefab == null) return;

        // Создаем запись в журнале
        var logEntry = Instantiate(logEntryPrefab, logContent);
        var textComponent = logEntry.GetComponent<TMP_Text>();

        if (textComponent != null)
        {
            textComponent.text = $"[{System.DateTime.Now:HH:mm}] {message}";
            textComponent.color = GetEventColor(eventType);
        }

        _logEntries.Enqueue(logEntry);
        _newEventsCount++;
        UpdateNewEventsCounter();

        // Ограничиваем количество записей
        if (_logEntries.Count > maxEntries)
        {
            var oldestEntry = _logEntries.Dequeue();
            Destroy(oldestEntry);
        }

        // Автоудаление через время
        StartCoroutine(AutoRemoveLogEntry(logEntry));

        // Показываем уведомление если журнал закрыт
        if (!logPanel.activeInHierarchy)
        {
            ShowNotification(message, eventType);
        }
    }

    private IEnumerator AutoRemoveLogEntry(GameObject logEntry)
    {
        yield return new WaitForSeconds(entryLifetime);

        if (logEntry != null)
        {
            // Находим и удаляем запись из очереди
            var newQueue = new Queue<GameObject>();
            while (_logEntries.Count > 0)
            {
                var entry = _logEntries.Dequeue();
                if (entry != logEntry)
                {
                    newQueue.Enqueue(entry);
                }
                else
                {
                    Destroy(entry);
                }
            }
            _logEntries = newQueue;
        }
    }

    private void RefreshEventLog()
    {
        if (!World.DefaultGameObjectInjectionWorld.IsCreated) return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var eventQuery = entityManager.CreateEntityQuery(typeof(GameEvent));
        var events = eventQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var eventEntity in events)
        {
            var gameEvent = entityManager.GetComponentData<GameEvent>(eventEntity);

            if (!gameEvent.Processed)
            {
                AddLogEntry(gameEvent.Description.ToString(), gameEvent.Type);

                // Помечаем как обработанное для UI
                var updatedEvent = gameEvent;
                updatedEvent.Processed = true;
                entityManager.SetComponentData(eventEntity, updatedEvent);
            }
        }

        events.Dispose();
    }

    private void ShowNotification(string message, EventType eventType)
    {
        // В реальной системе здесь можно сделать всплывающие уведомления
        Debug.Log($"🔔 {message}");
    }

    private Color GetEventColor(EventType eventType)
    {
        return eventType switch
        {
            EventType.BanditAttack => Color.red,
            EventType.WagonBreakdown => Color.yellow,
            EventType.WeatherStorm => Color.blue,
            EventType.TradeOpportunity => Color.green,
            EventType.RoadBlock => Color.magenta,
            _ => Color.white
        };
    }

    private void UpdateNewEventsCounter()
    {
        if (newEventsCounter != null)
        {
            newEventsCounter.text = _newEventsCount.ToString();
            newEventsCounter.gameObject.SetActive(_newEventsCount > 0);
        }
    }

    public void ClearLog()
    {
        foreach (var entry in _logEntries)
        {
            Destroy(entry);
        }
        _logEntries.Clear();
        _newEventsCount = 0;
        UpdateNewEventsCounter();
    }

    // Метод для вызова из других систем
    public void RequestRefresh()
    {
        _needsRefresh = true;
    }
}