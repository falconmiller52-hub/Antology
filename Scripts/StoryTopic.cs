using UnityEngine;

/// <summary>
/// Тема для радиосюжета. Создаётся через Assets → Create → Story → Topic.
///
/// Новый формат (ментальная карта):
/// - nodePlacements: все ноды этой темы с их позициями на карте.
/// - requiredChainLength: точная длина цепочки (обычно 4: cat 0→1→2→3).
///
/// Старый формат (blocks / PhraseOption) оставлен как [System.Obsolete]
/// для переходного периода — пока не удалены ссылки в legacy-сценах.
/// </summary>
[CreateAssetMenu(fileName = "NewStoryTopic", menuName = "Story/Topic")]
public class StoryTopic : ScriptableObject
{
    [Header("Info")]
    public string topicTitle = "Новая тема";

    [TextArea(2, 4)]
    public string topicDescription = "Описание темы для сюжета";

    // ===== НОВАЯ СИСТЕМА (ментальная карта) =====

    [Header("Mind Map Nodes")]
    [Tooltip("Все ноды, доступные в этой теме, с предустановленными позициями на карте.")]
    public StoryNodePlacement[] nodePlacements;

    [Header("Chain Requirements")]
    [Tooltip("Точная длина цепочки для валидного сюжета (обычно 4 = cat 0→1→2→3).")]
    public int requiredChainLength = 4;

    // ===== СТАРАЯ СИСТЕМА (legacy, для переходного периода) =====

    [Header("Legacy (old blocks system)")]
    [Tooltip("Устарело. Используется nodePlacements. Оставлено для совместимости.")]
    public StoryBlock[] blocks;

    // ===== Состояние =====

    [Header("State")]
    [HideInInspector]
    public bool isCompleted;

    public void ResetState()
    {
        isCompleted = false;
    }
}

/// <summary>
/// Размещение одной ноды на карте: ссылка на ScriptableObject + позиция на доске.
/// </summary>
[System.Serializable]
public class StoryNodePlacement
{
    public StoryNode node;

    [Tooltip("Позиция на карте (UI-координаты в RectTransform карты)")]
    public Vector2 position;
}
