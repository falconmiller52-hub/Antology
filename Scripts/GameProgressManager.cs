using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Синглтон прогрессии: дни, счётчик сюжетов, очки фракций.
/// Живёт между сценами (DontDestroyOnLoad).
///
/// Настройка:
/// 1. Создайте GameObject "GameProgress" в сцене MainMenu.
/// 2. Повесьте этот скрипт. Он переживёт смену сцен.
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int storiesPerDay = 3;
    [SerializeField] private int totalDays = 3;

    [Header("Scene Names")]
    [SerializeField] private string[] gameplayScenes = { "Gameplay1", "Gameplay2", "Gameplay3" };
    [SerializeField] private string intermediaScene = "Intermedia";
    [SerializeField] private string endingAScene = "EndingA";
    [SerializeField] private string endingBScene = "EndingB";

    // Текущее состояние
    public int CurrentDay { get; private set; } = 1;
    public int StoriesCompletedToday { get; private set; }
    public int FactionAScore { get; private set; }
    public int FactionBScore { get; private set; }
    public bool CanEndShift => StoriesCompletedToday >= storiesPerDay;
    public int StoriesPerDay => storiesPerDay;
    public int TotalDays => totalDays;

    // Сюжеты за текущий день (каждый сюжет = массив блоков)
    private List<string[]> _todayStoryBlocks = new List<string[]>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Полный сброс SO при первом создании (на случай грязного состояния в редакторе)
        ResetAllScriptableObjects();
        ResetDay();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // При входе в Gameplay сцену сбрасываем per-day SO состояния
        // (isCompleted у StoryTopic сбрасывается, т.к. каждая сцена имеет свои темы)
        // InterviewData.isCompleted НЕ сбрасываем — интервью per-game
        // RadioMessage.hasBeenPlayed НЕ сбрасываем — per-game
        if (scene.name.StartsWith("Gameplay"))
        {
            // Сбрасываем только темы сюжетов (они per-day)
            foreach (var topic in Resources.FindObjectsOfTypeAll<StoryTopic>())
                topic.ResetState();

            Debug.Log($"[GameProgress] Loaded {scene.name}. Day={CurrentDay}, " +
                      $"Stories={StoriesCompletedToday}, FactionA={FactionAScore}, FactionB={FactionBScore}");
        }
    }

    /// <summary>
    /// Вызывается из StoryEditorUI после отправки сюжета.
    /// blocks — массив заполненных фраз (по блокам).
    /// </summary>
    public void RegisterStory(string[] blocks, int factionAPoints, int factionBPoints)
    {
        if (StoriesCompletedToday < storiesPerDay)
        {
            _todayStoryBlocks.Add(blocks);
            StoriesCompletedToday++;
        }

        FactionAScore += factionAPoints;
        FactionBScore += factionBPoints;

        Debug.Log($"[GameProgress] Story registered. Day {CurrentDay}, " +
                  $"Stories: {StoriesCompletedToday}/{storiesPerDay}, " +
                  $"FactionA: {FactionAScore}, FactionB: {FactionBScore}");
    }

    /// <summary>
    /// Возвращает все блоки всех сюжетов за сегодня (для Intermedia).
    /// Каждый сюжет = массив строк (блоков), отображаются реплика за репликой.
    /// </summary>
    public List<string[]> GetTodayStoryBlocks()
    {
        return _todayStoryBlocks;
    }

    /// <summary>
    /// Завершает смену: переход к Intermedia.
    /// </summary>
    public void EndShift()
    {
        SceneManager.LoadScene(intermediaScene);
    }

    /// <summary>
    /// Вызывается из Intermedia после завершения вещания.
    /// </summary>
    public void OnBroadcastFinished()
    {
        CurrentDay++;

        if (CurrentDay > totalDays)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        else
        {
            ResetDay();
            int sceneIndex = Mathf.Clamp(CurrentDay - 1, 0, gameplayScenes.Length - 1);
            SceneManager.LoadScene(gameplayScenes[sceneIndex]);
        }
    }

    private void ResetDay()
    {
        StoriesCompletedToday = 0;
        _todayStoryBlocks.Clear();
    }

    /// <summary>
    /// Полный сброс для новой игры.
    /// </summary>
    public void ResetAll()
    {
        CurrentDay = 1;
        FactionAScore = 0;
        FactionBScore = 0;
        ResetDay();

        ResetAllScriptableObjects();

        // Сброс собранных ключей разведки
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
    }

#if UNITY_EDITOR
    /// <summary>
    /// Сбрасывает SO состояния при выходе из Play Mode в редакторе,
    /// чтобы isCompleted/hasBeenPlayed не застревали.
    /// </summary>
    private void OnApplicationQuit()
    {
        ResetAllScriptableObjects();
    }
#endif
}
