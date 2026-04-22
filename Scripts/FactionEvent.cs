using UnityEngine;

/// <summary>
/// Ивент фракции. Создаётся через Assets → Create → Event → Faction Event.
///
/// Проигрывается в сцене Events, когда в конце дня счёт соответствующей
/// фракции ≤ 0. Ивенты каждой фракции лежат в массиве FactionEvent[],
/// при триггере берётся первый с hasBeenShown == false.
/// </summary>
[CreateAssetMenu(fileName = "NewFactionEvent", menuName = "Event/Faction Event")]
public class FactionEvent : ScriptableObject
{
    [Header("Display")]
    public string title = "Новое событие";

    [TextArea(3, 8)]
    [Tooltip("Текст описания ДО выбора игрока.")]
    public string description;

    [Tooltip("Картинка события (на панели слева/сверху).")]
    public Sprite image;

    [Header("Choices")]
    [Tooltip("2-3 варианта действий. Кнопка [ПРОДОЛЖИТЬ] появится после выбора любого.")]
    public EventChoice[] choices;

    [Header("State")]
    [HideInInspector]
    public bool hasBeenShown;

    public void ResetState()
    {
        hasBeenShown = false;
    }
}

/// <summary>
/// Один вариант действия игрока в ответ на ивент.
/// </summary>
[System.Serializable]
public class EventChoice
{
    [Tooltip("Текст на кнопке")]
    public string buttonText;

    [TextArea(3, 8)]
    [Tooltip("Текст, который зачитывается после выбора (сменяет description)")]
    public string outcomeText;

    [Header("Cost (optional)")]
    [Tooltip("Стоимость выбора в очках фракции. Кнопка неактивна, если очков меньше cost.")]
    public FactionType costFaction = FactionType.None;
    [Tooltip("Сколько очков списать при выборе. Игнорируется, если costFaction = None.")]
    public int costAmount;

    [Header("Consequences — изменение очков фракций")]
    [Tooltip("Изменение очков фракции A при выборе этого ответа.")]
    public int factionADelta;
    public int factionBDelta;
    public int factionCDelta;
    public int factionDDelta;

    [Header("Death")]
    [Tooltip("Если true — вместо следующего ивента/Gameplay игра переходит в сцену Death.")]
    public bool triggersDeath;
}

public enum FactionType
{
    None,
    A,
    B,
    C,
    D
}
