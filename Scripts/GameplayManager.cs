using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет игровым процессом: пауза, возобновление, выход в меню.
/// Создайте в сцене Gameplay пустой GameObject "GameManager" и повесьте этот скрипт.
/// </summary>
public class GameplayManager : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Key pauseKey = Key.Escape;

    private bool _isPaused;

    private void Awake()
    {
        HidePauseMenu();
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        HidePauseMenu();
    }

    private void Start()
    {
        _isPaused = false;
        HidePauseMenu();

        if (pauseMenuPanel == null)
            Debug.LogError("[GameplayManager] pauseMenuPanel is NOT assigned! Drag it into the Inspector.");
        else
            Debug.Log($"[GameplayManager] pauseMenuPanel '{pauseMenuPanel.name}' active: {pauseMenuPanel.activeSelf}");
    }

    private void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        // Не открываем паузу, если открыто меню письма/газеты, компьютер или интервью
        if (Keyboard.current != null
            && Keyboard.current[pauseKey].wasPressedThisFrame
            && !InteractableItem.AnyMenuOpen
            && !ComputerManager.IsOpen
            && !InterviewManager.IsOpen)
            TogglePause();
    }

    /// <summary>
    /// Переключает состояние паузы.
    /// Можно привязать к кнопке паузы в UI, если нужна иконка.
    /// </summary>
    public void TogglePause()
    {
        if (_isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Привязывается к кнопке "Продолжить" в меню паузы.
    /// </summary>
    public void ResumeGame()
    {
        AudioManager.Instance?.PlayButtonClick();
        AudioManager.Instance?.RestoreMusic();
        AudioManager.Instance?.RestoreAmbient();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        _isPaused = false;
    }

    /// <summary>
    /// Привязывается к кнопке "Выйти в меню" в меню паузы.
    /// </summary>
    public void ReturnToMainMenu()
    {
        AudioManager.Instance?.PlayButtonClick();

        // Обязательно возвращаем timeScale перед сменой сцены!
        Time.timeScale = 1f;
        _isPaused = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void PauseGame()
    {
        AudioManager.Instance?.PlayPauseMenu();
        AudioManager.Instance?.DimMusic();
        AudioManager.Instance?.DimAmbient();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;
        _isPaused = true;
    }
}
