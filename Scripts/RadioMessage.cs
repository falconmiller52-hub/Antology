using UnityEngine;

/// <summary>
/// Данные радиосообщения: целевые частоты, реплики, день и голос.
/// Создаётся через Assets → Create → Radio → Message.
/// Каждое сообщение привязано к конкретному дню и проигрывается только один раз за игру.
/// </summary>
[CreateAssetMenu(fileName = "NewRadioMessage", menuName = "Radio/Message")]
public class RadioMessage : ScriptableObject
{
    [Header("Target Frequencies (0-1)")]
    [Range(0f, 1f)]
    public float targetFrequencyA = 0.3f;
    [Range(0f, 1f)]
    public float targetFrequencyB = 0.7f;

    [Header("Day Assignment")]
    [Tooltip("В какой день доступно это сообщение (1, 2, 3)")]
    public int dayNumber = 1;

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string[] lines;

    [Header("Intel Keys (one per line, auto-collected)")]
    [Tooltip("Ключи разведки — по одному на каждую реплику. Автоматически маркируются при прослушивании.")]
    public IntelKey[] lineIntelKeys;

    [Header("Voice")]
    [Tooltip("Звук голосовой дорожки (один звук проигрывается для каждого символа, как в Undertale)")]
    public AudioClip voiceBlip;
    [Tooltip("Pitch (тембр) для этого голоса. 1 = нормальный, >1 выше, <1 ниже")]
    [Range(0.5f, 2f)]
    public float voicePitch = 1f;

    [Header("Metadata")]
    public string senderName = "Неизвестный";
    [TextArea(1, 3)]
    public string description = "Перехваченное сообщение";

    [Header("State")]
    [HideInInspector]
    public bool hasBeenPlayed;

    public void ResetState()
    {
        hasBeenPlayed = false;
    }
}
