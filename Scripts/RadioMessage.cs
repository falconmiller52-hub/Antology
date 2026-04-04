using UnityEngine;

/// <summary>
/// Данные радиосообщения: целевые частоты и реплики.
/// Создаётся через Assets → Create → Radio → Message.
///
/// targetFrequencyA/B: значения от 0 до 1, соответствующие
/// нормализованным позициям двух ручек настройки.
/// </summary>
[CreateAssetMenu(fileName = "NewRadioMessage", menuName = "Radio/Message")]
public class RadioMessage : ScriptableObject
{
    [Header("Target Frequencies (0-1)")]
    [Range(0f, 1f)]
    public float targetFrequencyA = 0.3f;
    [Range(0f, 1f)]
    public float targetFrequencyB = 0.7f;

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string[] lines;

    [Header("Metadata")]
    public string senderName = "Неизвестный";
    [TextArea(1, 3)]
    public string description = "Перехваченное сообщение";
}
