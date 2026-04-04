using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управляет UI компьютера: переключение между каталогом тем и редактором сюжета.
/// Esc закрывает текущий экран (редактор → каталог → закрыть).
///
/// Настройка:
/// 1. Создайте Canvas (Screen Space - Overlay, Sort Order = 50).
/// 2. Внутри — два Panel: TopicListPanel и StoryEditorPanel.
/// 3. Повесьте этот скрипт на Canvas или отдельный GameObject.
/// 4. Назначьте панели, TopicListUI и StoryEditorUI в инспекторе.
/// </summary>
public class ComputerManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject topicListPanel;
    [SerializeField] private GameObject storyEditorPanel;

    [Header("UI Scripts")]
    [SerializeField] private TopicListUI topicListUI;
    [SerializeField] private StoryEditorUI storyEditorUI;

    [Header("Topics")]
    [SerializeField] private StoryTopic[] availableTopics;

    /// <summary>
    /// Статический флаг — открыт ли компьютер.
    /// GameplayManager проверяет его, чтобы не открывать паузу.
    /// </summary>
    public static bool IsOpen { get; private set; }

    private enum Screen { Closed, TopicList, StoryEditor }
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

    /// <summary>
    /// Открывает компьютер (каталог тем). Вызывается из ComputerPowerButton.
    /// </summary>
    public void OpenComputer()
    {
        if (IsOpen) return;

        ShowTopicList();
    }

    /// <summary>
    /// Вызывается из TopicListUI когда игрок выбрал тему.
    /// </summary>
    public void OpenStoryEditor(StoryTopic topic)
    {
        topicListPanel.SetActive(false);
        storyEditorPanel.SetActive(true);
        _currentScreen = Screen.StoryEditor;

        storyEditorUI.Initialize(topic, OnStorySubmitted);
    }

    /// <summary>
    /// Вызывается когда игрок нажал "Отправить" в редакторе.
    /// </summary>
    private void OnStorySubmitted(StoryTopic completedTopic)
    {
        completedTopic.isCompleted = true;

        AudioManager.Instance?.PlayButtonClick();

        // Возвращаемся в каталог тем
        ShowTopicList();
    }

    private void ShowTopicList()
    {
        storyEditorPanel.SetActive(false);
        topicListPanel.SetActive(true);
        _currentScreen = Screen.TopicList;
        IsOpen = true;

        topicListUI.Populate(availableTopics, this);
    }

    private void HandleEsc()
    {
        AudioManager.Instance?.PlayButtonClick();

        switch (_currentScreen)
        {
            case Screen.StoryEditor:
                // Из редактора — назад в каталог
                ShowTopicList();
                break;

            case Screen.TopicList:
                // Из каталога — закрыть компьютер
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
    }
}
