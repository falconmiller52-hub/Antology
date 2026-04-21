using UnityEngine;

/// <summary>
/// Данные интервью с одним НПС.
/// Создаётся через Assets → Create → Interview → Data.
///
/// Диалоговое дерево хранится как массив узлов.
/// Каждый узел имеет тип:
///  - Linear: NPC говорит, клик по панели → переход на nextNodeIndex.
///  - Choice: NPC говорит, затем показываются варианты ответов игрока.
///
/// Циклы делаются естественно: любой узел может указать nextNodeIndex
/// обратно на любой другой узел. Специальных режимов нет.
///
/// Индекс -1 в nextNodeIndex = конец интервью.
/// </summary>
[CreateAssetMenu(fileName = "NewInterview", menuName = "Interview/Data")]
public class InterviewData : ScriptableObject
{
    [Header("NPC Info")]
    public string npcName = "Неизвестный";
    public Sprite npcIcon;

    [TextArea(1, 2)]
    public string npcDescription = "Описание персонажа";

    [Header("Voice")]
    [Tooltip("Звук голосовой дорожки НПС (Undertale-стиль)")]
    public AudioClip voiceBlip;
    [Range(0.5f, 2f)]
    public float voicePitch = 1f;

    [Header("Dialogue Tree")]
    public DialogueNode[] nodes;

    [Header("State")]
    [HideInInspector]
    public bool isCompleted;

    public void ResetState()
    {
        isCompleted = false;
    }
}

/// <summary>
/// Тип узла диалога.
/// Linear — NPC говорит, после окончания клик ведёт на nextNodeIndex.
/// Choice — NPC говорит, затем показываются варианты ответов игрока.
/// </summary>
public enum DialogueNodeType
{
    Linear,
    Choice
}

/// <summary>
/// Один узел диалогового дерева.
/// </summary>
[System.Serializable]
public class DialogueNode
{
    [Tooltip("Реплика НПС")]
    [TextArea(2, 5)]
    public string npcLine;

    [Tooltip("Тип узла: Linear (автопереход по клику) или Choice (варианты ответа).")]
    public DialogueNodeType type = DialogueNodeType.Choice;

    [Tooltip("Куда перейти после клика. Используется только для Linear. -1 = конец интервью.")]
    public int nextNodeIndex = -1;

    [Tooltip("Варианты ответов игрока. Используется только для Choice.")]
    public PlayerResponse[] responses;

    [Tooltip("Ключ разведки, автоматически собираемый при показе этой реплики НПС")]
    public IntelKey intelKey;
}

/// <summary>
/// Вариант ответа игрока.
/// </summary>
[System.Serializable]
public class PlayerResponse
{
    [Tooltip("Текст ответа игрока")]
    [TextArea(1, 3)]
    public string text;

    [Tooltip("Индекс узла, на который ведёт этот ответ. -1 = конец интервью.")]
    public int nextNodeIndex = -1;

    [Tooltip("Если true — этот ответ отображается как [ЗАКОНЧИТЬ] и завершает интервью.")]
    public bool endsInterview;
}
