using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Управляет туториалом. День 1: письмо → газета → ПК → радио.
/// День 2: интервью. Блокирует зоны взаимодействия поэтапно.
///
/// Реплики проигрываются через панель (как радио диалог).
/// После каждого блока реплик разблокируется определённая зона.
/// Прогрессия идёт по событиям (получен интел, отправлен сюжет и т.д.).
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
        if (blocks == null || blocks.Length == 0)
        {
            // Нет туториала — всё разблокировано
            IsTutorialActive = false;
            return;
        }

        IsTutorialActive = true;
        InteractableItem.InteractionLocked = true;

        // Скрываем всё
        DisableAllZones();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        _currentBlockIndex = 0;
        StartBlock(_currentBlockIndex);
    }

    private void Update()
    {
        if (!IsTutorialActive) return;
        if (_waitingForEvent) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
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
        if (!IsTutorialActive || !_waitingForEvent) return;

        TutorialBlock currentBlock = blocks[_currentBlockIndex];
        if (currentBlock.waitForEvent == eventType)
        {
            _waitingForEvent = false;
            // Ждём пока все меню закроются перед показом следующего блока
            StartCoroutine(WaitForMenusClosedThenAdvance());
        }
    }

    private IEnumerator WaitForMenusClosedThenAdvance()
    {
        // Ждём пока закроются все меню (письма, газеты, ПК, интервью)
        yield return null; // Минимум 1 кадр
        while (InteractableItem.AnyMenuOpen || ComputerManager.IsOpen || InterviewManager.IsOpen)
            yield return null;

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
                // Ждём событие — разблокируем взаимодействие
                _waitingForEvent = true;
                InteractableItem.InteractionLocked = false;
                tutorialPanel.SetActive(false);
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
    RadioListened,
    InterviewCompleted
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
