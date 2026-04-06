using UnityEngine;

/// <summary>
/// Данные интервью с одним НПС.
/// Создаётся через Assets → Create → Interview → Data.
///
/// Диалоговое дерево хранится как массив узлов.
/// Каждый узел = реплика НПС + варианты ответов игрока.
/// Ответы ссылаются на следующие узлы по индексу.
/// Индекс -1 = конец интервью ([ЗАКОНЧИТЬ]).
/// Циклический узел: несколько ответов ведут обратно к тому же узлу (loopBackNodeIndex).
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
/// Один узел диалогового дерева.
/// </summary>
[System.Serializable]
public class DialogueNode
{
    [Tooltip("Реплика НПС")]
    [TextArea(2, 5)]
    public string npcLine;

    [Tooltip("Варианты ответов игрока")]
    public PlayerResponse[] responses;

    [Tooltip("Если true — после выбора любого ответа из цикла, НПС повторяет эту же реплику")]
    public bool isCyclic;

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

    [Tooltip("Индекс следующего узла. -1 = конец интервью. Для циклических — индекс промежуточного узла (реакция НПС перед возвратом)")]
    public int nextNodeIndex = -1;

    [Tooltip("Если true — это [ЗАКОНЧИТЬ], завершает интервью")]
    public bool endsInterview;
}
