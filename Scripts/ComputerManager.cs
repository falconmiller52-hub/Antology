using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управляет UI компьютера: анимация включения, каталог тем, редактор сюжета, кнопка завершения смены.
///
/// Настройка:
/// 1. Canvas (Screen Space - Overlay, Sort Order = 50).
/// 2. Внутри — TopicListPanel и StoryEditorPanel.
/// 3. Назначьте bootAnimation (на спрайте ПК) и endShiftButton.
/// </summary>
public class ComputerManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject topicListPanel;
    [SerializeField] private GameObject storyEditorPanel;

    [Header("UI Scripts")]
    [SerializeField] private TopicListUI topicListUI;
    [SerializeField] private StoryEditorUI storyEditorUI;
    [SerializeField] private EndShiftButton endShiftButton;

    [Header("Topics")]
    [SerializeField] private StoryTopic[] availableTopics;

    [Header("Boot Animation")]
    [SerializeField] private ComputerBootAnimation bootAnimation;

    public static bool IsOpen { get; private set; }

    private enum Screen { Closed, Booting, TopicList, StoryEditor }
    private Screen _currentScreen = Screen.Closed;

    private void Start()
    {
        CloseAll();
        bootAnimation?.ShowOff();
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            HandleEsc();
        }
    }

    /// <summary>
    /// Открывает компьютер. Сперва проигрывается анимация загрузки.
    /// </summary>
    public void OpenComputer()
    {
        if (IsOpen) return;

        IsOpen = true;
        _currentScreen = Screen.Booting;

        if (bootAnimation != null)
        {
            bootAnimation.PlayBoot(() => ShowTopicList());
        }
        else
        {
            ShowTopicList();
        }
    }

    public void OpenStoryEditor(StoryTopic topic)
    {
        topicListPanel.SetActive(false);
        storyEditorPanel.SetActive(true);
        _currentScreen = Screen.StoryEditor;

        storyEditorUI.Initialize(topic, OnStorySubmitted, OnEditorReturnToCatalog);
    }

    private void OnEditorReturnToCatalog()
    {
        ShowTopicList();
    }

    private void OnStorySubmitted(StoryTopic completedTopic)
    {
        completedTopic.isCompleted = true;
        AudioManager.Instance?.PlayPCButton();
        ShowTopicList();
    }

    private void ShowTopicList()
    {
        storyEditorPanel.SetActive(false);
        topicListPanel.SetActive(true);
        _currentScreen = Screen.TopicList;
        IsOpen = true;

        topicListUI.Populate(availableTopics, this);

        // Обновляем состояние кнопки "Завершить смену"
        if (endShiftButton != null)
            endShiftButton.UpdateState();
    }

    private void HandleEsc()
    {
        switch (_currentScreen)
        {
            case Screen.Booting:
                // Нельзя прервать загрузку
                break;

            case Screen.StoryEditor:
                // StoryEditorUI сам обрабатывает Esc
                break;

            case Screen.TopicList:
                AudioManager.Instance?.PlayPCButton();
                CloseAll();
                break;
        }
    }

    private void CloseAll()
    {
        if (topicListPanel != null) topicListPanel.SetActive(false);
        if (storyEditorPanel != null) storyEditorPanel.SetActive(false);

        _currentScreen = Screen.Closed;
        IsOpen = false;

        bootAnimation?.ShowOff();
    }
}
