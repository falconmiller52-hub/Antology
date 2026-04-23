using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Синглтон прогрессии: дни, счётчик сюжетов, очки фракций.
/// Живёт между сценами (DontDestroyOnLoad).
///
/// После последнего дня (CurrentDay > totalDays) проигрывается концовка:
///  - Выбирается фракция с максимальным числом очков.
///  - При равенстве приоритет: A > B > C > D.
///  - Загружается соответствующая сцена из endingScenes.
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    /// <summary>
    /// Гарантирует, что GameProgressManager существует, даже если игрок
    /// запустил сцену напрямую (минуя MainMenu). Вызывается Unity автоматически
    /// после загрузки первой сцены.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null) return;

        // Ищем существующий в сцене (из MainMenu, если нормально запустили)
        var existing = FindFirstObjectByType<GameProgressManager>();
        if (existing != null)
        {
            Debug.Log("[GameProgress] Using existing instance from scene.");
            return; // Awake сам выставит Instance
        }

        // Иначе создаём сами — запуск из середины (отладка)
        Debug.LogWarning("[GameProgress] No instance in scene — auto-spawning. " +
                         "Настройки будут дефолтные. Для нормальной игры запускайте из MainMenu.");
        var go = new GameObject("GameProgress (auto-spawned)");
        go.AddComponent<GameProgressManager>();
    }

    [Header("Game Settings")]
    [SerializeField] private int storiesPerDay = 3;
    [SerializeField] private int totalDays = 5;

    [Header("Scene Names")]
    [Tooltip("Сцены геймплея по дням. Длина массива должна быть >= totalDays.")]
    [SerializeField] private string[] gameplayScenes = { "Gameplay1", "Gameplay2", "Gameplay3", "Gameplay4", "Gameplay5" };
    [SerializeField] private string intermediaScene = "Intermedia";
    [SerializeField] private string eventsScene = "Events";
    [SerializeField] private string deathScene = "Death";

    [Header("Endings")]
    [Tooltip("Сцена концовки фракции A (имя из Build Settings, с пробелами если есть)")]
    [SerializeField] private string endingAScene = "Ending A";
    [SerializeField] private string endingBScene = "Ending B";
    [SerializeField] private string endingCScene = "Ending C";
    [SerializeField] private string endingDScene = "Ending D";

    [Header("Faction Events")]
    [Tooltip("Пул ивентов фракции A (триггерятся при A ≤ 0). " +
             "Берётся первый с hasBeenShown=false.")]
    [SerializeField] private FactionEvent[] factionAEvents;
    [SerializeField] private FactionEvent[] factionBEvents;
    [SerializeField] private FactionEvent[] factionCEvents;
    [SerializeField] private FactionEvent[] factionDEvents;

    public int CurrentDay { get; private set; } = 1;
    public int StoriesCompletedToday { get; private set; }
    public int FactionAScore { get; private set; }
    public int FactionBScore { get; private set; }
    public int FactionCScore { get; private set; }
    public int FactionDScore { get; private set; }
    public bool CanEndShift => StoriesCompletedToday >= storiesPerDay;
    public int StoriesPerDay => storiesPerDay;
    public int TotalDays => totalDays;

    private List<string[]> _todayStoryBlocks = new List<string[]>();

    // Делты очков по каждой ноде каждого сюжета за день. Структура совпадает
    // с _todayStoryBlocks (список сюжетов → массив блоков).
    // Используется Intermedia для прыгающего текста "+1A" на каждую реплику.
    private List<FactionDelta[]> _todayStoryDeltas = new List<FactionDelta[]>();

    // Снапшот очков на начало текущего дня. Обновляется при загрузке
    // каждой Gameplay-сцены. Используется ПК-меню для счётчика в углу.
    public int FactionAScoreAtDayStart { get; private set; }
    public int FactionBScoreAtDayStart { get; private set; }
    public int FactionCScoreAtDayStart { get; private set; }
    public int FactionDScoreAtDayStart { get; private set; }

    // Очередь ивентов, ожидающих проигрывания в сцене Events.
    // Формируется в OnBroadcastFinished, читается и очищается EventsScene.
    private List<FactionEvent> _pendingEvents = new List<FactionEvent>();
    public IReadOnlyList<FactionEvent> PendingEvents => _pendingEvents;

    public int GetFactionScore(FactionType faction)
    {
        switch (faction)
        {
            case FactionType.A: return FactionAScore;
            case FactionType.B: return FactionBScore;
            case FactionType.C: return FactionCScore;
            case FactionType.D: return FactionDScore;
            default: return 0;
        }
    }

    public void ApplyFactionDelta(FactionType faction, int delta)
    {
        switch (faction)
        {
            case FactionType.A: FactionAScore += delta; break;
            case FactionType.B: FactionBScore += delta; break;
            case FactionType.C: FactionCScore += delta; break;
            case FactionType.D: FactionDScore += delta; break;
        }
    }

    /// <summary>
    /// Применяет последствия выбора ивента: списание стоимости + дельты фракций.
    /// </summary>
    public void ApplyEventChoice(EventChoice choice)
    {
        if (choice == null) return;

        if (choice.costFaction != FactionType.None && choice.costAmount > 0)
            ApplyFactionDelta(choice.costFaction, -choice.costAmount);

        FactionAScore += choice.factionADelta;
        FactionBScore += choice.factionBDelta;
        FactionCScore += choice.factionCDelta;
        FactionDScore += choice.factionDDelta;

        Debug.Log($"[GameProgress] Event choice applied. " +
                  $"Cost: {choice.costFaction}={choice.costAmount}. " +
                  $"Deltas: A={choice.factionADelta} B={choice.factionBDelta} " +
                  $"C={choice.factionCDelta} D={choice.factionDDelta}. " +
                  $"New scores: A={FactionAScore} B={FactionBScore} C={FactionCScore} D={FactionDScore}");
    }

    public void LoadDeathScene()
    {
        Debug.Log($"[GameProgress] Death triggered. Loading '{deathScene}'.");
        if (string.IsNullOrEmpty(deathScene))
        {
            Debug.LogError("[GameProgress] Death scene name is not set in inspector.");
            return;
        }
        SceneManager.LoadScene(deathScene);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // DontDestroyOnLoad работает только для корневых GameObject.
        // Если объект был дочерним в MainMenu (лежал внутри SCRIPTS/UI/etc),
        // переносим его в корень сцены, иначе он умрёт при смене сцены.
        if (transform.parent != null)
        {
            Debug.LogWarning($"[GameProgress] Был дочерним '{transform.parent.name}'. " +
                             "Перемещаю в корень сцены для корректной работы DontDestroyOnLoad.");
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        ResetAllScriptableObjects();
        ResetDay();

        Debug.Log($"[GameProgress] Awake. totalDays={totalDays}, storiesPerDay={storiesPerDay}, " +
                  $"gameplayScenes=[{string.Join(", ", gameplayScenes)}], intermediaScene='{intermediaScene}', " +
                  $"endings=[{endingAScene}, {endingBScene}, {endingCScene}, {endingDScene}]");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Gameplay"))
        {
            foreach (var topic in Resources.FindObjectsOfTypeAll<StoryTopic>())
                topic.ResetState();

            // Снапшот очков на начало дня — для счётчика в ПК-меню.
            FactionAScoreAtDayStart = FactionAScore;
            FactionBScoreAtDayStart = FactionBScore;
            FactionCScoreAtDayStart = FactionCScore;
            FactionDScoreAtDayStart = FactionDScore;

            Debug.Log($"[GameProgress] Loaded {scene.name}. Day={CurrentDay}, " +
                      $"Stories={StoriesCompletedToday}, " +
                      $"Snapshot: A={FactionAScoreAtDayStart} B={FactionBScoreAtDayStart} " +
                      $"C={FactionCScoreAtDayStart} D={FactionDScoreAtDayStart}");
        }
    }

    public void RegisterStory(string[] blocks, int factionAPoints, int factionBPoints)
    {
        if (StoriesCompletedToday < storiesPerDay)
        {
            _todayStoryBlocks.Add(blocks);
            StoriesCompletedToday++;
        }

        FactionAScore += factionAPoints;
        FactionBScore += factionBPoints;

        Debug.Log($"[GameProgress][Legacy] Story registered. Stories: {StoriesCompletedToday}/{storiesPerDay}");
    }

    public void RegisterStoryMap(string[] broadcastTexts, int fA, int fB, int fC, int fD,
                                 FactionDelta[] perNodeDeltas = null)
    {
        if (StoriesCompletedToday < storiesPerDay)
        {
            _todayStoryBlocks.Add(broadcastTexts);
            _todayStoryDeltas.Add(perNodeDeltas ?? new FactionDelta[0]);
            StoriesCompletedToday++;
        }

        FactionAScore += fA;
        FactionBScore += fB;
        FactionCScore += fC;
        FactionDScore += fD;

        Debug.Log($"[GameProgress] StoryMap registered. Day {CurrentDay}, " +
                  $"Stories: {StoriesCompletedToday}/{storiesPerDay}, " +
                  $"A:{FactionAScore} B:{FactionBScore} C:{FactionCScore} D:{FactionDScore}");
    }

    public List<string[]> GetTodayStoryBlocks() => _todayStoryBlocks;
    public List<FactionDelta[]> GetTodayStoryDeltas() => _todayStoryDeltas;

    public void EndShift()
    {
        Debug.Log($"[GameProgress] EndShift called. Loading '{intermediaScene}'.");
        SceneManager.LoadScene(intermediaScene);
    }

    public void OnBroadcastFinished()
    {
        Debug.Log($"[GameProgress] OnBroadcastFinished. Day={CurrentDay}, totalDays={totalDays}, " +
                  $"scores A={FactionAScore} B={FactionBScore} C={FactionCScore} D={FactionDScore}");

        // Собираем очередь ивентов по фракциям с ≤0 очков (в порядке A→B→C→D).
        _pendingEvents.Clear();
        TryQueueEventsForFaction(FactionType.A, FactionAScore, factionAEvents);
        TryQueueEventsForFaction(FactionType.B, FactionBScore, factionBEvents);
        TryQueueEventsForFaction(FactionType.C, FactionCScore, factionCEvents);
        TryQueueEventsForFaction(FactionType.D, FactionDScore, factionDEvents);

        if (_pendingEvents.Count > 0)
        {
            Debug.Log($"[GameProgress] {_pendingEvents.Count} event(s) queued. Loading '{eventsScene}'.");
            SceneManager.LoadScene(eventsScene);
            return;
        }

        AdvanceAfterEvents();
    }

    /// <summary>
    /// Вызывается EventsScene после того, как все ивенты (из _pendingEvents)
    /// проиграны и отреагированы. Переходит к следующему дню или к концовке.
    /// Также вызывается OnBroadcastFinished напрямую, если ивентов нет.
    /// </summary>
    public void AdvanceAfterEvents()
    {
        CurrentDay++;

        if (CurrentDay > totalDays)
        {
            LoadEndingScene();
            return;
        }

        ResetDay();
        int sceneIndex = CurrentDay - 1;
        if (sceneIndex < 0 || sceneIndex >= gameplayScenes.Length)
        {
            Debug.LogError($"[GameProgress] No gameplay scene configured for day {CurrentDay}.");
            return;
        }
        string nextScene = gameplayScenes[sceneIndex];
        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogError($"[GameProgress] gameplayScenes[{sceneIndex}] is empty.");
            return;
        }
        Debug.Log($"[GameProgress] Loading next gameplay scene: '{nextScene}' (index {sceneIndex}).");
        SceneManager.LoadScene(nextScene);
    }

    private void TryQueueEventsForFaction(FactionType faction, int score, FactionEvent[] pool)
    {
        if (score > 0) return;
        if (pool == null) return;

        // Берём первый неиспользованный ивент
        foreach (var ev in pool)
        {
            if (ev == null) continue;
            if (ev.hasBeenShown) continue;
            _pendingEvents.Add(ev);
            break; // по одному ивенту на фракцию за день
        }
    }

    /// <summary>
    /// Определяет победившую фракцию (максимум очков, при равенстве A>B>C>D)
    /// и загружает соответствующую сцену концовки.
    /// </summary>
    private void LoadEndingScene()
    {
        Debug.Log($"[GameProgress] All {totalDays} days complete. " +
                  $"Final scores: A={FactionAScore}, B={FactionBScore}, C={FactionCScore}, D={FactionDScore}");

        // Приоритет A>B>C>D: используем строгое неравенство при сравнении,
        // чтобы при равенстве побеждала фракция, упомянутая раньше.
        string winner = "A";
        int bestScore = FactionAScore;
        string bestScene = endingAScene;

        if (FactionBScore > bestScore) { winner = "B"; bestScore = FactionBScore; bestScene = endingBScene; }
        if (FactionCScore > bestScore) { winner = "C"; bestScore = FactionCScore; bestScene = endingCScene; }
        if (FactionDScore > bestScore) { winner = "D"; bestScore = FactionDScore; bestScene = endingDScene; }

        if (string.IsNullOrEmpty(bestScene))
        {
            Debug.LogError($"[GameProgress] Ending scene for faction {winner} is not set in inspector.");
            return;
        }

        Debug.Log($"[GameProgress] Winning faction: {winner} ({bestScore} pts). Loading ending scene '{bestScene}'.");
        SceneManager.LoadScene(bestScene);
    }

    private void ResetDay()
    {
        StoriesCompletedToday = 0;
        _todayStoryBlocks.Clear();
        _todayStoryDeltas.Clear();
    }

    public void ResetAll()
    {
        CurrentDay = 1;
        FactionAScore = 0;
        FactionBScore = 0;
        FactionCScore = 0;
        FactionDScore = 0;
        FactionAScoreAtDayStart = 0;
        FactionBScoreAtDayStart = 0;
        FactionCScoreAtDayStart = 0;
        FactionDScoreAtDayStart = 0;
        ResetDay();
        _pendingEvents.Clear();
        StoryMapUI.ClearAllCachedStates();

        ResetAllScriptableObjects();

        if (IntelManager.Instance != null)
            IntelManager.Instance.ResetAll();
    }

    private void ResetAllScriptableObjects()
    {
        foreach (var msg in Resources.FindObjectsOfTypeAll<RadioMessage>())
            msg.ResetState();
        foreach (var topic in Resources.FindObjectsOfTypeAll<StoryTopic>())
            topic.ResetState();
        foreach (var interview in Resources.FindObjectsOfTypeAll<InterviewData>())
            interview.ResetState();
        foreach (var ev in Resources.FindObjectsOfTypeAll<FactionEvent>())
            ev.ResetState();
    }

#if UNITY_EDITOR
    private void OnApplicationQuit()
    {
        ResetAllScriptableObjects();
    }
#endif
}
