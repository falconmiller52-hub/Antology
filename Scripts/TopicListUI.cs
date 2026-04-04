using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Экран каталога тем. Генерирует кнопки тем из массива StoryTopic.
/// Завершённые темы не показываются.
///
/// Настройка:
/// 1. На TopicListPanel повесьте этот скрипт.
/// 2. Внутри Panel создайте Vertical Layout Group контейнер (topicContainer).
/// 3. Создайте префаб кнопки темы (topicButtonPrefab):
///    - Button с дочерним TextMeshProUGUI.
///    - Размер под вертикальный список.
/// </summary>
public class TopicListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform topicContainer;
    [SerializeField] private GameObject topicButtonPrefab;

    private ComputerManager _computerManager;

    /// <summary>
    /// Заполняет список тем. Вызывается из ComputerManager.
    /// </summary>
    public void Populate(StoryTopic[] topics, ComputerManager manager)
    {
        _computerManager = manager;

        // Очищаем старые кнопки
        foreach (Transform child in topicContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаём кнопку для каждой незавершённой темы
        foreach (StoryTopic topic in topics)
        {
            if (topic.isCompleted) continue;

            GameObject buttonObj = Instantiate(topicButtonPrefab, topicContainer);

            // Устанавливаем текст
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = topic.topicTitle;

            // Привязываем клик
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                StoryTopic capturedTopic = topic; // Захват для замыкания
                button.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    _computerManager.OpenStoryEditor(capturedTopic);
                });
            }
        }
    }
}
