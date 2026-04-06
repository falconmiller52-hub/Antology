using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private string gameplayScene = "Gameplay";
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

    // Сюжеты за текущий день (тексты для вещания)
    private string[] _todayStoryTexts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResetDay();
    }

    /// <summary>
    /// Вызывается из StoryEditorUI после отправки сюжета.
    /// </summary>
    public void RegisterStory(string assembledText, int factionAPoints, int factionBPoints)
    {
        if (StoriesCompletedToday < storiesPerDay)
        {
            _todayStoryTexts[StoriesCompletedToday] = assembledText;
            StoriesCompletedToday++;
        }

        FactionAScore += factionAPoints;
        FactionBScore += factionBPoints;

        Debug.Log($"[GameProgress] Story registered. Day {CurrentDay}, " +
                  $"Stories: {StoriesCompletedToday}/{storiesPerDay}, " +
                  $"FactionA: {FactionAScore}, FactionB: {FactionBScore}");
    }

    /// <summary>
    /// Возвращает тексты сюжетов за сегодня (для сцены Intermedia).
    /// </summary>
    public string[] GetTodayStories()
    {
        return _todayStoryTexts;
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
            // Конец игры — выбираем концовку
            string endingScene = (FactionBScore > FactionAScore) ? endingBScene : endingAScene;
            SceneManager.LoadScene(endingScene);
        }
        else
        {
            ResetDay();
            SceneManager.LoadScene(gameplayScene);
        }
    }

    private void ResetDay()
    {
        StoriesCompletedToday = 0;
        _todayStoryTexts = new string[storiesPerDay];
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
    }
}
