using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Контроллер радиоприёмника.
/// Управляет включением/выключением, определением сигнала,
/// звуковым миксом (помехи/голоса) и показом диалоговой панели.
///
/// Настройка:
/// 1. Создайте GameObject "Radio" со SpriteRenderer (спрайт приёмника).
/// 2. Повесьте этот скрипт.
/// 3. Дочерние объекты: два RadioDial (триггера), RadioPowerButton, DialoguePanel.
/// 4. Назначьте RadioMessage (ScriptableObject) с частотами и репликами.
/// 5. Два AudioSource: один для помех (staticSource), один для голосов (voiceSource).
/// </summary>
public class RadioReceiver : MonoBehaviour
{
    [Header("Dials")]
    [SerializeField] private RadioDial dialA;
    [SerializeField] private RadioDial dialB;

    [Header("Power")]
    [SerializeField] private RadioPowerButton powerButton;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource staticSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip staticNoiseClip;
    [SerializeField] private AudioClip voiceMumbleClip;

    [Header("Signal Settings")]
    [Tooltip("Насколько близко значения должны быть к целевым для полного сигнала (0-1)")]
    [SerializeField] private float signalTolerance = 0.08f;
    [Tooltip("Радиус, в котором голоса начинают проступать (0-1)")]
    [SerializeField] private float hearingRadius = 0.25f;

    [Header("Dialogue")]
    [SerializeField] private RadioDialoguePanel dialoguePanel;

    [Header("Current Message")]
    [SerializeField] private RadioMessage currentMessage;

    private bool _isPoweredOn;
    private bool _signalLocked;
    private bool _dialogueActive;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.Hide();

        SetPoweredState(false);
    }

    private void Update()
    {
        if (!_isPoweredOn) return;
        if (_dialogueActive) return;

        UpdateSignal();
    }

    /// <summary>
    /// Вызывается RadioPowerButton при нажатии.
    /// </summary>
    public void TogglePower()
    {
        SetPoweredState(!_isPoweredOn);
    }

    private void SetPoweredState(bool on)
    {
        _isPoweredOn = on;

        if (dialA != null) dialA.SetInteractable(on);
        if (dialB != null) dialB.SetInteractable(on);

        if (on)
        {
            // Используем клипы из инспектора, или из AudioManager как fallback
            AudioClip staticClip = staticNoiseClip;
            AudioClip voiceClip = voiceMumbleClip;

            if (staticClip == null && AudioManager.Instance != null)
                staticClip = AudioManager.Instance.GetRadioStaticClip();
            if (voiceClip == null && AudioManager.Instance != null)
                voiceClip = AudioManager.Instance.GetRadioVoiceClip();

            if (staticSource != null && staticClip != null)
            {
                staticSource.clip = staticClip;
                staticSource.loop = true;
                staticSource.volume = 1f;
                staticSource.Play();
            }

            if (voiceSource != null && voiceClip != null)
            {
                voiceSource.clip = voiceClip;
                voiceSource.loop = true;
                voiceSource.volume = 0f;
                voiceSource.Play();
            }
        }
        else
        {
            // Выключаем всё
            if (staticSource != null) staticSource.Stop();
            if (voiceSource != null) voiceSource.Stop();

            if (dialoguePanel != null)
                dialoguePanel.Hide();

            _signalLocked = false;
            _dialogueActive = false;
        }
    }

    private void UpdateSignal()
    {
        if (currentMessage == null) return;

        // Вычисляем расстояние каждого триггера от целевой частоты (0 = попал точно)
        float distA = Mathf.Abs(dialA.NormalizedValue - currentMessage.targetFrequencyA);
        float distB = Mathf.Abs(dialB.NormalizedValue - currentMessage.targetFrequencyB);

        // Общее расстояние от сигнала (0 = идеально настроено)
        float totalDist = (distA + distB) / 2f;

        // Сила сигнала: 1 = чисто слышно, 0 = только помехи
        float signalStrength = 0f;
        if (totalDist < hearingRadius)
        {
            signalStrength = 1f - (totalDist / hearingRadius);
        }

        // Микшируем звук
        if (staticSource != null)
            staticSource.volume = 1f - signalStrength * 0.85f;

        if (voiceSource != null)
            voiceSource.volume = signalStrength;

        // Если оба значения достаточно близки — сигнал пойман
        if (distA <= signalTolerance && distB <= signalTolerance)
        {
            if (!_signalLocked)
                LockSignal();
        }
        else
        {
            _signalLocked = false;
        }
    }

    private void LockSignal()
    {
        _signalLocked = true;
        _dialogueActive = true;

        // Убираем помехи, оставляем голос
        if (staticSource != null)
            staticSource.volume = 0.05f;
        if (voiceSource != null)
            voiceSource.volume = 1f;

        // Блокируем вращение триггеров пока идёт диалог
        if (dialA != null) dialA.SetInteractable(false);
        if (dialB != null) dialB.SetInteractable(false);

        // Показываем диалог
        if (dialoguePanel != null && currentMessage != null)
        {
            dialoguePanel.Show(currentMessage.lines, OnDialogueFinished);
        }
    }

    private void OnDialogueFinished()
    {
        _dialogueActive = false;
        _signalLocked = false;

        // Восстанавливаем управление триггерами
        if (dialA != null) dialA.SetInteractable(true);
        if (dialB != null) dialB.SetInteractable(true);

        // Убираем голос, возвращаем помехи
        if (voiceSource != null)
            voiceSource.volume = 0f;
        if (staticSource != null)
            staticSource.volume = 1f;

        if (dialoguePanel != null)
            dialoguePanel.Hide();
    }

    /// <summary>
    /// Устанавливает новое сообщение для поиска.
    /// Вызывается из системы прогрессии/дней.
    /// </summary>
    public void SetMessage(RadioMessage message)
    {
        currentMessage = message;
        _signalLocked = false;
        _dialogueActive = false;
    }
}
