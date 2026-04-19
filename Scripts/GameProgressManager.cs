using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Синглтон прогрессии: дни, счётчик сюжетов, очки фракций.
/// Живёт между сценами (DontDestroyOnLoad).
///
/// 4 фракции (A/B/C/D) — используются новой системой ментальной карты.
/// Старая система (RegisterStory с 2 фракциями) сохранена для совместимости.
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
    public int FactionCScore { get; private set; }
    public int FactionDScore { get; private set; }
    public bool CanEndShift => StoriesCompletedToday >= storiesPerDay;
    public int StoriesPerDay => storiesPerDay;
    public int TotalDays => totalDays;

    // Сюжеты за текущий день.
    // Каждый сюжет = массив строк (текстов broadcast'а по ячейкам цепочки).
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

        ResetAllScriptableObjects();
        ResetDay();
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

            Debug.Log($"[GameProgress] Loaded {scene.name}. Day={CurrentDay}, " +
                      $"Stories={StoriesCompletedToday}, " +
                      $"A={FactionAScore} B={FactionBScore} C={FactionCScore} D={FactionDScore}");
        }
    }

    /// <summary>
    /// Устаревший метод: старая система с 2 фракциями.
    /// Оставлен для совместимости с легаси-сценами.
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

        Debug.Log($"[GameProgress][Legacy] Story registered. Day {CurrentDay}, " +
                  $"Stories: {StoriesCompletedToday}/{storiesPerDay}, " +
                  $"A:{FactionAScore} B:{FactionBScore}");
    }

    /// <summary>
    /// Новый метод для системы ментальной карты.
    /// broadcastTexts — тексты по ячейкам цепочки (в порядке от cat 0 до cat N).
    /// </summary>
    public void RegisterStoryMap(string[] broadcastTexts, int fA, int fB, int fC, int fD)
    {
        if (StoriesCompletedToday < storiesPerDay)
        {
            _todayStoryBlocks.Add(broadcastTexts);
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

    /// <summary>
    /// Возвращает все блоки всех сюжетов за сегодня (для Intermedia).
    /// </summary>
    public List<string[]> GetTodayStoryBlocks()
    {
        return _todayStoryBlocks;
    }

    public void EndShift()
    {
        SceneManager.LoadScene(intermediaScene);
    }

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

    public void ResetAll()
    {
        CurrentDay = 1;
        FactionAScore = 0;
        FactionBScore = 0;
        FactionCScore = 0;
        FactionDScore = 0;
        ResetDay();

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
    }

#if UNITY_EDITOR
    private void OnApplicationQuit()
    {
        ResetAllScriptableObjects();
    }
#endif
}
