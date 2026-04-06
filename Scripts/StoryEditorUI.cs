using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Экран редактора сюжета. Разделён на две части:
/// - Левая: блоки-пропуски, заполняемые по порядку.
/// - Правая: варианты фраз для текущего блока.
///
/// Esc — отменяет последнюю вставку (возврат к предыдущему блоку).
/// Если текущий блок — первый, Esc возвращает в каталог тем.
///
/// Настройка:
/// 1. На StoryEditorPanel повесьте этот скрипт.
/// 2. Левая панель: Vertical Layout Group контейнер (blocksContainer).
///    - Spacing = 4, Padding = 4-8. Child Force Expand Width = true.
/// 3. Правая панель: Vertical Layout Group контейнер (phrasesContainer).
///    - Spacing = 4, Padding = 4-8. Child Force Expand Width = true.
/// 4. Два префаба: blockSlotPrefab, phraseButtonPrefab.
/// 5. submitButton — деактивирована по умолчанию.
/// </summary>
public class StoryEditorUI : MonoBehaviour
{
    [Header("Left Panel — Blocks")]
    [SerializeField] private Transform blocksContainer;
    [SerializeField] private GameObject blockSlotPrefab;

    [Header("Right Panel — Phrases")]
    [SerializeField] private Transform phrasesContainer;
    [SerializeField] private GameObject phraseButtonPrefab;

    [Header("UI Elements")]
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI topicTitleText;

    [Header("Block Colors")]
    [SerializeField] private Color emptyBlockColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color activeBlockColor = new Color(0.2f, 0.4f, 0.2f, 1f);
    [SerializeField] private Color filledBlockColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);

    [Header("Block Text Colors")]
    [SerializeField] private Color emptyTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color activeTextColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color filledTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Phrase Button Colors")]
    [SerializeField] private Color phraseNormalColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color phraseHoverColor = new Color(0.25f, 0.4f, 0.25f, 1f);
    [SerializeField] private Color phrasePressedColor = new Color(0.15f, 0.3f, 0.15f, 1f);
    [SerializeField] private Color phraseTextNormalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color phraseTextHoverColor = new Color(0.9f, 1f, 0.9f, 1f);

    private StoryTopic _currentTopic;
    private System.Action<StoryTopic> _onSubmitted;
    private System.Action _onReturnToCatalog;

    private int _currentBlockIndex;
    private string[] _filledPhrases;
    private int[] _selectedPhraseIndices;
    private GameObject[] _blockSlots;
    private bool _isActive;

    private int _totalFactionA;
    private int _totalFactionB;

    /// <summary>
    /// Инициализирует редактор для заданной темы.
    /// </summary>
    public void Initialize(StoryTopic topic, System.Action<StoryTopic> onSubmitted, System.Action onReturnToCatalog = null)
    {
        _currentTopic = topic;
        _onSubmitted = onSubmitted;
        _onReturnToCatalog = onReturnToCatalog;
        _currentBlockIndex = 0;
        _filledPhrases = new string[topic.blocks.Length];
        _selectedPhraseIndices = new int[topic.blocks.Length];
        _blockSlots = new GameObject[topic.blocks.Length];
        _isActive = true;
        _totalFactionA = 0;
        _totalFactionB = 0;

        if (topicTitleText != null)
            topicTitleText.text = topic.topicTitle;

        submitButton.gameObject.SetActive(false);
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(OnSubmit);

        BuildBlocks();
        ShowPhrasesForCurrentBlock();
    }

    private void Update()
    {
        if (!_isActive) return;

        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            HandleEsc();
        }
    }

    private void HandleEsc()
    {
        AudioManager.Instance?.PlayKeyboardTyping();

        if (_currentBlockIndex > 0)
        {
            submitButton.gameObject.SetActive(false);

            _currentBlockIndex--;

            // Вычитаем очки отменённой фразы
            int undoneIndex = _selectedPhraseIndices[_currentBlockIndex];
            StoryBlock block = _currentTopic.blocks[_currentBlockIndex];
            PhraseOption undoneOption = block.phraseOptions[undoneIndex];
            _totalFactionA -= undoneOption.factionAPoints;
            _totalFactionB -= undoneOption.factionBPoints;

            _filledPhrases[_currentBlockIndex] = null;

            ResetBlockSlot(_currentBlockIndex);
            HighlightCurrentBlock();
            ShowPhrasesForCurrentBlock();
        }
        else
        {
            _isActive = false;
            _onReturnToCatalog?.Invoke();
        }
    }

    private void BuildBlocks()
    {
        foreach (Transform child in blocksContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < _currentTopic.blocks.Length; i++)
        {
            GameObject slot = Instantiate(blockSlotPrefab, blocksContainer);
            _blockSlots[i] = slot;

            TextMeshProUGUI slotText = slot.GetComponentInChildren<TextMeshProUGUI>();
            Image slotBg = slot.GetComponent<Image>();

            if (slotText != null)
            {
                slotText.text = _currentTopic.blocks[i].blockLabel + ": ___";
                slotText.color = (i == 0) ? activeTextColor : emptyTextColor;
            }

            if (slotBg != null)
                slotBg.color = (i == 0) ? activeBlockColor : emptyBlockColor;
        }
    }

    private void ShowPhrasesForCurrentBlock()
    {
        foreach (Transform child in phrasesContainer)
            Destroy(child.gameObject);

        if (_currentBlockIndex >= _currentTopic.blocks.Length) return;

        StoryBlock block = _currentTopic.blocks[_currentBlockIndex];

        for (int i = 0; i < block.phraseOptions.Length; i++)
        {
            GameObject btnObj = Instantiate(phraseButtonPrefab, phrasesContainer);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = block.phraseOptions[i].text;
                btnText.color = phraseTextNormalColor;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock colors = btn.colors;
                colors.normalColor = phraseNormalColor;
                colors.highlightedColor = phraseHoverColor;
                colors.pressedColor = phrasePressedColor;
                colors.selectedColor = phraseHoverColor;
                btn.colors = colors;

                int capturedIndex = i;
                TextMeshProUGUI capturedText = btnText;
                btn.onClick.AddListener(() => OnPhraseSelected(capturedIndex));

                AddTextHoverEffect(btnObj, capturedText);
            }
        }
    }

    private void AddTextHoverEffect(GameObject btnObj, TextMeshProUGUI text)
    {
        var trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((_) => { if (text != null) text.color = phraseTextHoverColor; });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((_) => { if (text != null) text.color = phraseTextNormalColor; });
        trigger.triggers.Add(exitEntry);
    }

    private void OnPhraseSelected(int phraseIndex)
    {
        AudioManager.Instance?.PlayKeyboardTyping();

        StoryBlock block = _currentTopic.blocks[_currentBlockIndex];
        PhraseOption option = block.phraseOptions[phraseIndex];

        _filledPhrases[_currentBlockIndex] = option.text;
        _selectedPhraseIndices[_currentBlockIndex] = phraseIndex;
        _totalFactionA += option.factionAPoints;
        _totalFactionB += option.factionBPoints;

        UpdateBlockSlot(_currentBlockIndex, option.text);

        _currentBlockIndex++;

        if (_currentBlockIndex >= _currentTopic.blocks.Length)
        {
            ClearPhrases();
            submitButton.gameObject.SetActive(true);
        }
        else
        {
            HighlightCurrentBlock();
            ShowPhrasesForCurrentBlock();
        }
    }

    private void UpdateBlockSlot(int index, string phrase)
    {
        if (_blockSlots[index] == null) return;

        TextMeshProUGUI slotText = _blockSlots[index].GetComponentInChildren<TextMeshProUGUI>();
        Image slotBg = _blockSlots[index].GetComponent<Image>();

        if (slotText != null)
        {
            slotText.text = _currentTopic.blocks[index].blockLabel + ": " + phrase;
            slotText.color = filledTextColor;
        }

        if (slotBg != null)
            slotBg.color = filledBlockColor;
    }

    private void ResetBlockSlot(int index)
    {
        if (_blockSlots[index] == null) return;

        TextMeshProUGUI slotText = _blockSlots[index].GetComponentInChildren<TextMeshProUGUI>();
        Image slotBg = _blockSlots[index].GetComponent<Image>();

        if (slotText != null)
        {
            slotText.text = _currentTopic.blocks[index].blockLabel + ": ___";
            slotText.color = activeTextColor;
        }

        if (slotBg != null)
            slotBg.color = activeBlockColor;
    }

    private void HighlightCurrentBlock()
    {
        for (int i = 0; i < _blockSlots.Length; i++)
        {
            if (_blockSlots[i] == null) continue;

            Image bg = _blockSlots[i].GetComponent<Image>();
            TextMeshProUGUI text = _blockSlots[i].GetComponentInChildren<TextMeshProUGUI>();

            if (i == _currentBlockIndex)
            {
                if (bg != null) bg.color = activeBlockColor;
                if (text != null) text.color = activeTextColor;
            }
            else if (i < _currentBlockIndex)
            {
                if (bg != null) bg.color = filledBlockColor;
                if (text != null) text.color = filledTextColor;
            }
            else
            {
                if (bg != null) bg.color = emptyBlockColor;
                if (text != null) text.color = emptyTextColor;
            }
        }
    }

    private void ClearPhrases()
    {
        foreach (Transform child in phrasesContainer)
            Destroy(child.gameObject);
    }

    private void OnSubmit()
    {
        _isActive = false;

        // Собираем текст сюжета из заполненных фраз
        string assembledText = string.Join(" ", _filledPhrases);

        // Регистрируем в системе прогрессии
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.RegisterStory(assembledText, _totalFactionA, _totalFactionB);

        _onSubmitted?.Invoke(_currentTopic);
    }
}
