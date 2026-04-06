using UnityEngine;

/// <summary>
/// Один вариант фразы с очками фракций.
/// </summary>
[System.Serializable]
public class PhraseOption
{
    [TextArea(2, 4)]
    public string text;

    [Tooltip("Очки для фракции A (пропаганда/режим)")]
    public int factionAPoints;

    [Tooltip("Очки для фракции B (оппозиция)")]
    public int factionBPoints;

    [Tooltip("Ключ разведки, необходимый для разблокировки этой фразы. Пустой = всегда доступна.")]
    public IntelKey requiredIntelKey;
}

/// <summary>
/// Один блок-пропуск в сюжете.
/// Содержит варианты фраз, из которых игрок выбирает одну.
/// </summary>
[System.Serializable]
public class StoryBlock
{
    [Tooltip("Подсказка, что должно быть в этом блоке (например: 'Вступление', 'Описание события')")]
    public string blockLabel = "Блок";

    [Tooltip("Варианты фраз для этого блока. Игрок выбирает одну.")]
    public PhraseOption[] phraseOptions;
}
