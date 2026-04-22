using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер сцены Events.
/// Проигрывает последовательно все ивенты из GameProgressManager.PendingEvents.
///
/// Для каждого ивента:
///  1. Показывает title + картинку + description (с typewriter-эффектом).
///  2. Показывает 2-3 кнопки с вариантами действий.
///     Кнопки с costFaction != None становятся неактивны, если очков < costAmount;
///     показывается подсказка "(требуется X ★F)".
///  3. После выбора кнопки: применяет последствия, показывает outcomeText
///     (снова typewriter), показывает кнопку "Продолжить".
///  4. Клик "Продолжить" → следующий ивент или выход из сцены.
///
/// Если у выбора triggersDeath=true — после показа outcomeText при "Продолжить"
/// загружается сцена Death вместо следующего ивента.
///
/// Настройка сцены:
/// 1. Canvas с панелью события.
/// 2. Image для картинки, TMP для title, TMP для основного текста.
/// 3. Контейнер для кнопок выбора (choicesContainer) + префаб кнопки.
/// 4. Отдельная кнопка "Продолжить" (continueButton), выключена по умолчанию.
/// 5. SceneFadeIn/SceneFadeOut — опционально.
/// </summary>
public class EventsScene : MonoBehaviour
{
    [Header("UI — Event Panel")]
    [SerializeField] private Image eventImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("UI — Choices")]
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("UI — Continue")]
    [SerializeField] private Button continueButton;

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.03f;
    [SerializeField] private AudioSource blipSource;
    [SerializeField] private AudioClip voiceBlip;
    [Range(0.5f, 2f)]
    [SerializeField] private float voicePitch = 1f;
    [SerializeField] private int charsPerBlip = 2;

    [Header("Choice Button Labels")]
    [Tooltip("Символы для фракций в подписи стоимости. Должны соответствовать порядку A, B, C, D.")]
    [SerializeField] private string[] factionSymbols = { "★A", "★B", "★C", "★D" };

    private int _currentEventIndex;
    private List<FactionEvent> _queue;
    private bool _isTyping;
    private bool _skipTyping;
    private bool _deathPendingAfterContinue;
    private Coroutine _typeCoroutine;

    private void Start()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("[Events] GameProgressManager.Instance is null — cannot run events scene.");
            return;
        }

        _queue = new List<FactionEvent>(GameProgressManager.Instance.PendingEvents);

        if (_queue.Count == 0)
        {
            Debug.Log("[Events] No events queued — advancing immediately.");
            GameProgressManager.Instance.AdvanceAfterEvents();
            return;
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        _currentEventIndex = 0;
        ShowEvent(_queue[_currentEventIndex]);
    }

    private void Update()
    {
        if (_isTyping && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            _skipTyping = true;
    }

    private void ShowEvent(FactionEvent ev)
    {
        _deathPendingAfterContinue = false;

        if (titleText != null) titleText.text = ev.title;
        if (eventImage != null)
        {
            eventImage.sprite = ev.image;
            eventImage.enabled = ev.image != null;
        }

        ClearChoices();
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        StartTyping(ev.description, () => ShowChoices(ev));
    }

    private void ShowChoices(FactionEvent ev)
    {
        ClearChoices();

        if (ev.choices == null || ev.choices.Length == 0)
        {
            Debug.LogWarning($"[Events] Event '{ev.title}' has no choices. Showing Continue.");
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            return;
        }

        for (int i = 0; i < ev.choices.Length; i++)
        {
            EventChoice choice = ev.choices[i];
            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);

            // Ищем TMP-тексты внутри префаба — первый для основного текста,
            // второй (если есть) для подсказки стоимости.
            TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
                texts[0].text = choice.buttonText;

            bool hasCost = choice.costFaction != FactionType.None && choice.costAmount > 0;
            bool canAfford = true;
            if (hasCost)
            {
                int currentScore = GameProgressManager.Instance.GetFactionScore(choice.costFaction);
                canAfford = currentScore >= choice.costAmount;
            }

            if (texts.Length > 1)
            {
                if (hasCost)
                    texts[1].text = $"(требуется {choice.costAmount} {GetFactionSymbol(choice.costFaction)})";
                else
                    texts[1].text = "";
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = canAfford;
                EventChoice captured = choice;
                btn.onClick.AddListener(() => OnChoiceSelected(captured));
            }
        }
    }

    private void OnChoiceSelected(EventChoice choice)
    {
        AudioManager.Instance?.PlayButtonClick();
        ClearChoices();

        GameProgressManager.Instance.ApplyEventChoice(choice);
        _deathPendingAfterContinue = choice.triggersDeath;

        StartTyping(choice.outcomeText, () =>
        {
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
        });
    }

    private void OnContinueClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (_deathPendingAfterContinue)
        {
            GameProgressManager.Instance.LoadDeathScene();
            return;
        }

        // Помечаем текущий ивент как показанный
        _queue[_currentEventIndex].hasBeenShown = true;

        _currentEventIndex++;
        if (_currentEventIndex >= _queue.Count)
        {
            Debug.Log("[Events] All events processed. Advancing.");
            GameProgressManager.Instance.AdvanceAfterEvents();
            return;
        }

        ShowEvent(_queue[_currentEventIndex]);
    }

    private void StartTyping(string line, System.Action onComplete)
    {
        _skipTyping = false;
        _isTyping = true;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(line, onComplete));
    }

    private IEnumerator TypeText(string line, System.Action onComplete)
    {
        if (bodyText == null) { _isTyping = false; onComplete?.Invoke(); yield break; }
        if (line == null) line = "";

        bodyText.text = "";
        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping) { bodyText.text = line; break; }
            bodyText.text = line.Substring(0, i + 1);

            if (voiceBlip != null && blipSource != null
                && !char.IsWhiteSpace(line[i]) && i % charsPerBlip == 0)
            {
                blipSource.pitch = voicePitch + Random.Range(-0.05f, 0.05f);
                blipSource.PlayOneShot(voiceBlip);
            }

            yield return new WaitForSeconds(typeSpeed);
        }
        _isTyping = false;
        onComplete?.Invoke();
    }

    private void ClearChoices()
    {
        if (choicesContainer == null) return;
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);
    }

    private string GetFactionSymbol(FactionType faction)
    {
        switch (faction)
        {
            case FactionType.A: return factionSymbols.Length > 0 ? factionSymbols[0] : "A";
            case FactionType.B: return factionSymbols.Length > 1 ? factionSymbols[1] : "B";
            case FactionType.C: return factionSymbols.Length > 2 ? factionSymbols[2] : "C";
            case FactionType.D: return factionSymbols.Length > 3 ? factionSymbols[3] : "D";
            default: return "";
        }
    }
}
