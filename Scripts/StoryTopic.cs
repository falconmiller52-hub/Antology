using UnityEngine;

/// <summary>
/// Тема для радиосюжета. Создаётся через Assets → Create → Story → Topic.
/// Содержит название темы, описание и набор блоков-пропусков,
/// которые игрок заполняет фразами.
/// </summary>
[CreateAssetMenu(fileName = "NewStoryTopic", menuName = "Story/Topic")]
public class StoryTopic : ScriptableObject
{
    [Header("Info")]
    public string topicTitle = "Новая тема";

    [TextArea(2, 4)]
    public string topicDescription = "Описание темы для сюжета";

    [Header("Story Blocks")]
    [Tooltip("Блоки-пропуски, которые игрок заполняет по порядку")]
    public StoryBlock[] blocks;

    [Header("State")]
    [HideInInspector]
    public bool isCompleted;

    /// <summary>
    /// Сбрасывает состояние (для начала нового дня).
    /// </summary>
    public void ResetState()
    {
        isCompleted = false;
    }
}
