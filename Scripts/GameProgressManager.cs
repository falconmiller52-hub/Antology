using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Синглтон прогрессии: дни, счётчик сюжетов, очки фракций.
/// Живёт между сценами (DontDestroyOnLoad).
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

        Debug.Log($"[GameProgress] Awake. totalDays={totalDays}, storiesPerDay={storiesPerDay}, " +
                  $"gameplayScenes=[{string.Join(", ", gameplayScenes)}], intermediaScene='{intermediaScene}'");
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

    public List<string[]> GetTodayStoryBlocks() => _todayStoryBlocks;

    public void EndShift()
    {
        Debug.Log($"[GameProgress] EndShift called. Loading '{intermediaScene}'.");
        SceneManager.LoadScene(intermediaScene);
    }

    public void OnBroadcastFinished()
    {
        Debug.Log($"[GameProgress] OnBroadcastFinished. Before increment: CurrentDay={CurrentDay}, totalDays={totalDays}");

        CurrentDay++;

        Debug.Log($"[GameProgress] After increment: CurrentDay={CurrentDay}. " +
                  $"Condition 'CurrentDay > totalDays' = {CurrentDay > totalDays}");

        if (CurrentDay > totalDays)
        {
            Debug.Log($"[GameProgress] All days complete — game over. " +
                      $"(If this is unexpected, check totalDays in inspector — should be 3 for 3-day game.)");
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
            string nextScene = gameplayScenes[sceneIndex];
            Debug.Log($"[GameProgress] Loading next gameplay scene: '{nextScene}' (index {sceneIndex}).");
            SceneManager.LoadScene(nextScene);
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
