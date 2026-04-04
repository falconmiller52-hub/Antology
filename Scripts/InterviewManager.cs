using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управляет системой интервью: каталог персоналий и экран диалога.
/// Esc закрывает каталог (но НЕ диалог — диалог завершается только через [ЗАКОНЧИТЬ]).
///
/// Настройка:
/// 1. Canvas (Screen Space - Overlay, Sort Order = 60).
/// 2. Внутри — CatalogPanel и DialoguePanel.
/// 3. Повесьте этот скрипт, назначьте панели и массив интервью.
/// </summary>
public class InterviewManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject catalogPanel;
    [SerializeField] private GameObject dialoguePanel;

    [Header("UI Scripts")]
    [SerializeField] private InterviewCatalogUI catalogUI;
    [SerializeField] private InterviewDialogueUI dialogueUI;

    [Header("Interviews")]
    [SerializeField] private InterviewData[] availableInterviews;

    public static bool IsOpen { get; private set; }

    private enum Screen { Closed, Catalog, Dialogue }
    private Screen _currentScreen = Screen.Closed;

    private void Start()
    {
        CloseAll();
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            HandleEsc();
        }
    }

    public void OpenCatalog()
    {
        if (IsOpen) return;
        ShowCatalog();
    }

    public void StartInterview(InterviewData data)
    {
        catalogPanel.SetActive(false);
        dialoguePanel.SetActive(true);
        _currentScreen = Screen.Dialogue;

        dialogueUI.Initialize(data, OnInterviewFinished);
    }

    private void OnInterviewFinished(InterviewData completed)
    {
        completed.isCompleted = true;
        AudioManager.Instance?.PlayButtonClick();

        // Возвращаемся в каталог
        ShowCatalog();
    }

    private void ShowCatalog()
    {
        dialoguePanel.SetActive(false);
        catalogPanel.SetActive(true);
        _currentScreen = Screen.Catalog;
        IsOpen = true;

        catalogUI.Populate(availableInterviews, this);
    }

    private void HandleEsc()
    {
        switch (_currentScreen)
        {
            case Screen.Catalog:
                // Из каталога — закрыть
                AudioManager.Instance?.PlayButtonClick();
                CloseAll();
                break;

            case Screen.Dialogue:
                // Диалог нельзя закрыть по Esc — только через [ЗАКОНЧИТЬ]
                break;
        }
    }

    private void CloseAll()
    {
        if (catalogPanel != null) catalogPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        _currentScreen = Screen.Closed;
        IsOpen = false;
    }
}
