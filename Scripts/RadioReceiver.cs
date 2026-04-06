using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Контроллер радиоприёмника.
/// Поддерживает несколько сообщений, фильтрацию по дню, одноразовое проигрывание.
/// Передаёт voiceBlip и voicePitch из RadioMessage в RadioDialoguePanel.
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
    [SerializeField] private float signalTolerance = 0.08f;
    [SerializeField] private float hearingRadius = 0.25f;

    [Header("Dialogue")]
    [SerializeField] private RadioDialoguePanel dialoguePanel;

    [Header("All Messages")]
    [SerializeField] private RadioMessage[] allMessages;

    private bool _isPoweredOn;
    private bool _signalLocked;
    private bool _dialogueActive;
    private RadioMessage _activeMessage;

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
            int currentDay = GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentDay : 1;
            int available = 0;
            if (allMessages != null)
            {
                foreach (var msg in allMessages)
                {
                    if (msg != null && !msg.hasBeenPlayed && msg.dayNumber == currentDay)
                        available++;
                }
            }
            Debug.Log($"[RadioReceiver] Powered ON. Day={currentDay}, Messages total={allMessages?.Length ?? 0}, Available={available}");

            AudioClip staticClip = staticNoiseClip;
            if (staticClip == null && AudioManager.Instance != null)
                staticClip = AudioManager.Instance.GetRadioStaticClip();

            if (staticSource != null && staticClip != null)
            {
                staticSource.clip = staticClip;
                staticSource.loop = true;
                staticSource.volume = 1f;
                staticSource.Play();
            }

            AudioClip voiceClip = voiceMumbleClip;
            if (voiceClip == null && AudioManager.Instance != null)
                voiceClip = AudioManager.Instance.GetRadioVoiceClip();

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
            if (staticSource != null) staticSource.Stop();
            if (voiceSource != null) voiceSource.Stop();
            if (dialoguePanel != null) dialoguePanel.Hide();
            _signalLocked = false;
            _dialogueActive = false;
        }
    }

    private void UpdateSignal()
    {
        int currentDay = GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentDay : 1;

        // Находим ближайшее непроигранное сообщение для текущего дня
        RadioMessage closest = null;
        float closestDist = float.MaxValue;

        foreach (RadioMessage msg in allMessages)
        {
            if (msg == null) continue;
            if (msg.hasBeenPlayed || msg.dayNumber != currentDay) continue;

            float distA = Mathf.Abs(dialA.NormalizedValue - msg.targetFrequencyA);
            float distB = Mathf.Abs(dialB.NormalizedValue - msg.targetFrequencyB);
            float totalDist = (distA + distB) / 2f;

            if (totalDist < closestDist)
            {
                closestDist = totalDist;
                closest = msg;
            }
        }

        _activeMessage = closest;

        if (_activeMessage == null)
        {
            // Нет доступных сообщений — только помехи
            if (staticSource != null) staticSource.volume = 1f;
            if (voiceSource != null) voiceSource.volume = 0f;
            return;
        }

        float signalStrength = 0f;
        if (closestDist < hearingRadius)
            signalStrength = 1f - (closestDist / hearingRadius);

        if (staticSource != null)
            staticSource.volume = 1f - signalStrength * 0.85f;
        if (voiceSource != null)
            voiceSource.volume = signalStrength;

        float distACheck = Mathf.Abs(dialA.NormalizedValue - _activeMessage.targetFrequencyA);
        float distBCheck = Mathf.Abs(dialB.NormalizedValue - _activeMessage.targetFrequencyB);

        if (distACheck <= signalTolerance && distBCheck <= signalTolerance)
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
        InteractableItem.InteractionLocked = true;

        // Останавливаем зацикленные звуки — теперь голос идёт через voice blip
        if (staticSource != null) staticSource.Stop();
        if (voiceSource != null) voiceSource.Stop();

        if (dialA != null) dialA.SetInteractable(false);
        if (dialB != null) dialB.SetInteractable(false);

        if (dialoguePanel != null && _activeMessage != null)
        {
            dialoguePanel.Show(
                _activeMessage.lines,
                OnDialogueFinished,
                _activeMessage.voiceBlip,
                _activeMessage.voicePitch,
                _activeMessage.lineIntelKeys
            );
        }
    }

    private void OnDialogueFinished()
    {
        if (_activeMessage != null)
            _activeMessage.hasBeenPlayed = true;

        _dialogueActive = false;
        _signalLocked = false;
        InteractableItem.InteractionLocked = false;

        if (dialA != null) dialA.SetInteractable(true);
        if (dialB != null) dialB.SetInteractable(true);

        if (voiceSource != null) voiceSource.volume = 0f;
        if (staticSource != null) staticSource.volume = 1f;

        if (dialoguePanel != null) dialoguePanel.Hide();

        TutorialManager.Instance?.OnTutorialEvent(TutorialEventType.RadioListened);
    }
}
