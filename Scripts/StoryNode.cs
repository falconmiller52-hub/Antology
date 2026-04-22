using UnityEngine;

/// <summary>
/// Ячейка ментальной карты сюжета.
/// Создаётся через Assets → Create → Story → Node.
///
/// Категории: 0 = корневая (тема), 1, 2, 3 — последующие звенья цепочки.
/// Связи допустимы только 0→1, 1→2, 2→3.
///
/// Каждая нода требует IntelKey — игрок должен собрать ключ, чтобы
/// ячейка стала интерактивной. На одном ключе может висеть несколько нод.
///
/// Поле description используется для:
///  - заголовка/описания ноды в редакторе,
///  - реплики в эфире (Intermedia) — если нода попала в собранный сюжет.
/// </summary>
[CreateAssetMenu(fileName = "NewStoryNode", menuName = "Story/Node")]
public class StoryNode : ScriptableObject
{
    [Header("Display")]
    public string label = "Новая ячейка";

    [TextArea(2, 5)]
    [Tooltip("Описание ноды. Зачитывается в эфире Intermedia, если нода попала в сюжет.")]
    public string description;

    [Header("Category")]
    [Tooltip("0 = корневая (тема), 1/2/3 — звенья. Связи только cat X → cat X+1.")]
    [Range(0, 3)]
    public int category = 0;

    [Header("Unlock")]
    [Tooltip("Ключ разведки, разблокирующий эту ноду. Обязателен.")]
    public IntelKey requiredIntelKey;

    [Header("Faction Scores (выплачиваются при отправке сюжета)")]
    public int factionAPoints;
    public int factionBPoints;
    public int factionCPoints;
    public int factionDPoints;
}
