using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Управляет туториалом. День 1 (новый флоу):
/// Блок 0 (реплики) → письмо (ждём IntelCollected + закрытие меню) →
/// Блок 2 (реплики) → газета (ждём IntelCollected + закрытие меню) →
/// Блок 4 (реплики) → радио (ждём RadioMessageCompleted) →
/// Блок 6 (реплики) → микрофон (ждём InterviewCompleted) →
/// Блок 8 (реплики) → ПК (ждём StorySubmitted) →
/// Финальный блок → конец туториала.
///
/// Реплики проигрываются через панель в стиле радио (voiceBlip + typewriter).
/// Если игрок закрыл письмо/газету не отметив ключ — панель остаётся скрытой,
/// туториал продолжит ждать IntelCollected при повторном взаимодействии.
///
/// Настройка:
/// 1. В каждой Gameplay сцене создайте GameObject "TutorialManager".
/// 2. Назначьте панель текста, блоки реплик, ссылки на интерактивные объекты.
/// 3. Для дней без туториала — не добавляйте этот скрипт.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private AudioSource blipSource;

    [Header("Voice")]
    [SerializeField] private AudioClip voiceBlip;
    [Range(0.5f, 2f)]
    [SerializeField] private float voicePitch = 1f;

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.04f;
    [SerializeField] private int charsPerBlip = 2;

    [Header("Tutorial Blocks")]
    [SerializeField] private TutorialBlock[] blocks;

    [Header("Enable Conditions")]
    [Tooltip("Если true — туториал запустится только если GameProgressManager.CurrentDay == 1. " +
             "Полезно оставить один и тот же TutorialManager-префаб в нескольких сценах.")]
    [SerializeField] private bool enableOnlyInFirstDay = true;

    [Header("Interaction Zones")]
    [SerializeField] private GameObject[] letterObjects;
    [SerializeField] private GameObject[] newspaperObjects;
    [SerializeField] private GameObject computerObject;
    [SerializeField] private GameObject radioObject;
    [SerializeField] private GameObject microphoneObject;

    public static TutorialManager Instance { get; private set; }
    public bool IsTutorialActive { get; private set; }

    private int _currentBlockIndex;
    private int _currentLineIndex;
    private bool _isTyping;
    private bool _skipTyping;
    private bool _waitingForEvent;
    private bool _eventReceived;
    private Coroutine _typeCoroutine;

    private void Awake()
    {
        Instance = this;

        if (blipSource == null)
        {
            blipSource = gameObject.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Проверка: должен ли туториал вообще запускаться в этой сцене?
        bool shouldDisable = false;
        string disableReason = null;

        if (blocks == null || blocks.Length == 0)
        {
            shouldDisable = true;
            disableReason = "no blocks configured";
        }
        else if (enableOnlyInFirstDay
                 && GameProgressManager.Instance != null
                 && GameProgressManager.Instance.CurrentDay > 1)
        {
            shouldDisable = true;
            disableReason = $"CurrentDay={GameProgressManager.Instance.CurrentDay} > 1 and enableOnlyInFirstDay=true";
        }

        if (shouldDisable)
        {
            Debug.Log($"[Tutorial] Disabled: {disableReason}.");
            IsTutorialActive = false;
            // Гарантируем, что ничего не залипло от возможных предыдущих запусков.
            InteractableItem.InteractionLocked = false;
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            // Все зоны должны быть доступны сразу.
            UnlockZone(TutorialZone.All);
            return;
        }

        Debug.Log($"[Tutorial] Starting tutorial with {blocks.Length} blocks.");
        IsTutorialActive = true;
        InteractableItem.InteractionLocked = true;

        DisableAllZones();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
        else
            Debug.LogError("[Tutorial] tutorialPanel is NOT assigned!");

        _currentBlockIndex = 0;
        StartBlock(_currentBlockIndex);
    }

    private void Update()
    {
        if (!IsTutorialActive) return;
        if (_waitingForEvent) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Не перехватываем клик, если курсор над интерактивным предметом или UI-меню
            if (InteractableItem.AnyMenuOpen) return;

            if (_isTyping)
                _skipTyping = true;
            else
                AdvanceLine();
        }
    }

    /// <summary>
    /// Вызывается внешними системами когда событие произошло.
    /// Например: IntelManager вызывает после сбора ключа,
    /// ComputerManager после отправки сюжета и т.д.
    /// </summary>
    public void OnTutorialEvent(TutorialEventType eventType)
    {
        Debug.Log($"[Tutorial] Event received: {eventType}, active={IsTutorialActive}, waiting={_waitingForEvent}, " +
                  $"currentBlock={_currentBlockIndex}, expectedEvent={(_currentBlockIndex < blocks.Length ? blocks[_currentBlockIndex].waitForEvent.ToString() : "N/A")}");

        if (!IsTutorialActive || !_waitingForEvent) return;

        TutorialBlock currentBlock = blocks[_currentBlockIndex];
        if (currentBlock.waitForEvent == eventType)
        {
            Debug.Log($"[Tutorial] Event matched! Waiting for menus to close...");
            _eventReceived = true;
            StartCoroutine(WaitForMenusClosedThenAdvance());
        }
    }

    private IEnumerator WaitForMenusClosedThenAdvance()
    {
        yield return null;

        Debug.Log($"[Tutorial] Checking menus: AnyMenuOpen={InteractableItem.AnyMenuOpen}, " +
                  $"ComputerOpen={ComputerManager.IsOpen}, InterviewOpen={InterviewManager.IsOpen}, " +
                  $"InteractionLocked={InteractableItem.InteractionLocked}");

        // Ждём пока закроются все меню И завершится любое текущее радиовзаимодействие.
        // RadioReceiver во время диалога держит InteractionLocked=true, так что ждём и его тоже.
        while (InteractableItem.AnyMenuOpen
               || ComputerManager.IsOpen
               || InterviewManager.IsOpen
               || InteractableItem.InteractionLocked)
        {
            yield return null;
        }

        Debug.Log($"[Tutorial] Menus closed. Advancing to block {_currentBlockIndex + 1}");

        _waitingForEvent = false;
        _eventReceived = false;
        InteractableItem.InteractionLocked = true;

        _currentBlockIndex++;
        if (_currentBlockIndex >= blocks.Length)
            EndTutorial();
        else
            StartBlock(_currentBlockIndex);
    }

    private void StartBlock(int index)
    {
        TutorialBlock block = blocks[index];
        _currentLineIndex = 0;
        _eventReceived = false;

        // Разблокируем зону для этого блока
        UnlockZone(block.unlockZone);

        if (block.lines != null && block.lines.Length > 0)
        {
            tutorialPanel.SetActive(true);
            ShowLine(block.lines[0]);
        }
        else
        {
            // Нет реплик — сразу ждём событие
            if (block.waitForEvent != TutorialEventType.None)
            {
                _waitingForEvent = true;
                InteractableItem.InteractionLocked = false;
                tutorialPanel.SetActive(false);
            }
        }
    }

    private void AdvanceLine()
    {
        TutorialBlock block = blocks[_currentBlockIndex];
        _currentLineIndex++;

        if (_currentLineIndex >= block.lines.Length)
        {
            // Блок реплик закончился
            if (block.waitForEvent != TutorialEventType.None)
            {
                // Ждём событие — разблокируем взаимодействие, прячем панель
                _waitingForEvent = true;
                InteractableItem.InteractionLocked = false;
                tutorialPanel.SetActive(false);
                Debug.Log($"[Tutorial] Block {_currentBlockIndex} lines done. Waiting for {block.waitForEvent}. InteractionLocked=false.");
            }
            else
            {
                // Нет события — сразу следующий блок
                _currentBlockIndex++;
                if (_currentBlockIndex >= blocks.Length)
                    EndTutorial();
                else
                    StartBlock(_currentBlockIndex);
            }
        }
        else
        {
            ShowLine(block.lines[_currentLineIndex]);
        }
    }

    private void ShowLine(string line)
    {
        _skipTyping = false;
        _isTyping = true;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string line)
    {
        tutorialText.text = "";
        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping) { tutorialText.text = line; break; }
            tutorialText.text = line.Substring(0, i + 1);
            if (voiceBlip != null && blipSource != null
                && !char.IsWhiteSpace(line[i]) && i % charsPerBlip == 0)
            {
                blipSource.pitch = voicePitch + Random.Range(-0.05f, 0.05f);
                blipSource.PlayOneShot(voiceBlip);
            }
            yield return new WaitForSeconds(typeSpeed);
        }
        _isTyping = false;
    }

    private void UnlockZone(TutorialZone zone)
    {
        switch (zone)
        {
            case TutorialZone.Letters:
                SetObjectsActive(letterObjects, true);
                break;
            case TutorialZone.Newspapers:
                SetObjectsActive(newspaperObjects, true);
                break;
            case TutorialZone.Computer:
                if (computerObject != null) computerObject.SetActive(true);
                break;
            case TutorialZone.Radio:
                if (radioObject != null) radioObject.SetActive(true);
                break;
            case TutorialZone.Microphone:
                if (microphoneObject != null) microphoneObject.SetActive(true);
                break;
            case TutorialZone.All:
                SetObjectsActive(letterObjects, true);
                SetObjectsActive(newspaperObjects, true);
                if (computerObject != null) computerObject.SetActive(true);
                if (radioObject != null) radioObject.SetActive(true);
                if (microphoneObject != null) microphoneObject.SetActive(true);
                break;
        }
    }

    private void DisableAllZones()
    {
        SetObjectsActive(letterObjects, false);
        SetObjectsActive(newspaperObjects, false);
        if (computerObject != null) computerObject.SetActive(false);
        if (radioObject != null) radioObject.SetActive(false);
        if (microphoneObject != null) microphoneObject.SetActive(false);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (var obj in objects)
            if (obj != null) obj.SetActive(active);
    }

    private void EndTutorial()
    {
        IsTutorialActive = false;
        InteractableItem.InteractionLocked = false;
        tutorialPanel.SetActive(false);

        // Разблокируем всё
        UnlockZone(TutorialZone.All);
    }
}

/// <summary>
/// Один блок туториала: реплики + событие для продолжения + зона для разблокировки.
/// </summary>
[System.Serializable]
public class TutorialBlock
{
    [TextArea(2, 4)]
    public string[] lines;

    [Tooltip("Какое событие ждём после реплик (None = сразу следующий блок)")]
    public TutorialEventType waitForEvent = TutorialEventType.None;

    [Tooltip("Какую зону разблокировать перед этим блоком")]
    public TutorialZone unlockZone = TutorialZone.None;
}

public enum TutorialEventType
{
    None,
    IntelCollected,
    StorySubmitted,
    RadioMessageCompleted,
    InterviewCompleted,

    // Устаревшее — оставлено для совместимости со старыми сериализованными значениями.
    // Новые блоки используйте RadioMessageCompleted.
    RadioListened
}

public enum TutorialZone
{
    None,
    Letters,
    Newspapers,
    Computer,
    Radio,
    Microphone,
    All
}
