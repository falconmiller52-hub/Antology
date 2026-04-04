using UnityEngine;

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
    [TextArea(2, 4)]
    public string[] phraseOptions;
}
