using UnityEngine;

/// <summary>
/// Один вариант фразы с очками фракций и ветвлением.
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

    [Tooltip("Индекс следующего блока. -1 = следующий по порядку (по умолчанию). Используй для ветвления.")]
    public int nextBlockIndex = -1;
}

/// <summary>
/// Один блок-пропуск в сюжете.
/// Содержит варианты фраз, из которых игрок выбирает одну.
/// Блоки образуют граф: каждая фраза может вести к определённому следующему блоку.
/// </summary>
[System.Serializable]
public class StoryBlock
{
    [Tooltip("Подсказка, что должно быть в этом блоке (например: 'Вступление', 'Описание события')")]
    public string blockLabel = "Блок";

    [Tooltip("Варианты фраз для этого блока. Игрок выбирает одну.")]
    public PhraseOption[] phraseOptions;

    [Tooltip("Это финальный блок (после него — кнопка Отправить). Если false и nextBlockIndex=-1, идёт следующий по порядку.")]
    public bool isFinalBlock;
}
