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

    [Header("Locked Phrase Colors")]
    [SerializeField] private Color lockedTextColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color lockedBlockColor = new Color(0.12f, 0.12f, 0.12f, 0.6f);

    private StoryTopic _currentTopic;
    private System.Action<StoryTopic> _onSubmitted;
    private System.Action _onReturnToCatalog;

    private int _currentBlockIndex;
    private string[] _filledPhrases;
    private int[] _selectedPhraseIndices;
    private GameObject[] _blockSlots;
    private bool _isActive;
    private System.Collections.Generic.List<int> _blockPath = new System.Collections.Generic.List<int>();

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
        _blockPath.Clear();

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

        if (_blockPath.Count > 0)
        {
            submitButton.gameObject.SetActive(false);

            // Возвращаемся к предыдущему блоку в пути
            int previousBlock = _blockPath[_blockPath.Count - 1];
            _blockPath.RemoveAt(_blockPath.Count - 1);

            // Вычитаем очки отменённой фразы
            int undoneIndex = _selectedPhraseIndices[previousBlock];
            StoryBlock block = _currentTopic.blocks[previousBlock];
            PhraseOption undoneOption = block.phraseOptions[undoneIndex];
            _totalFactionA -= undoneOption.factionAPoints;
            _totalFactionB -= undoneOption.factionBPoints;

            _filledPhrases[previousBlock] = null;
            _currentBlockIndex = previousBlock;

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
            PhraseOption option = block.phraseOptions[i];
            bool isUnlocked;
            if (option.requiredIntelKey == null)
                isUnlocked = true; // Нет ключа — всегда доступна
            else if (IntelManager.Instance == null)
                isUnlocked = false; // IntelManager не найден — заблокировано
            else
                isUnlocked = IntelManager.Instance.HasKey(option.requiredIntelKey);

            GameObject btnObj = Instantiate(phraseButtonPrefab, phrasesContainer);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = isUnlocked ? option.text : "???";
                btnText.color = isUnlocked ? phraseTextNormalColor : lockedTextColor;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                if (isUnlocked)
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
                else
                {
                    btn.interactable = false;
                    ColorBlock colors = btn.colors;
                    colors.disabledColor = lockedBlockColor;
                    btn.colors = colors;
                }
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

        // Track filled data
        _filledPhrases[_currentBlockIndex] = option.text;
        _selectedPhraseIndices[_currentBlockIndex] = phraseIndex;
        _totalFactionA += option.factionAPoints;
        _totalFactionB += option.factionBPoints;

        // Track path for undo
        _blockPath.Add(_currentBlockIndex);

        UpdateBlockSlot(_currentBlockIndex, option.text);

        // Determine next block
        if (block.isFinalBlock)
        {
            // Final block — show submit
            ClearPhrases();
            submitButton.gameObject.SetActive(true);
        }
        else if (option.nextBlockIndex >= 0 && option.nextBlockIndex < _currentTopic.blocks.Length)
        {
            // Branch to specific block
            _currentBlockIndex = option.nextBlockIndex;
            HighlightCurrentBlock();
            ShowPhrasesForCurrentBlock();
        }
        else
        {
            // Default: next sequential block
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

        // Собираем только заполненные блоки (по пути, не все)
        var filledBlocks = new System.Collections.Generic.List<string>();
        foreach (int idx in _blockPath)
        {
            if (_filledPhrases[idx] != null)
                filledBlocks.Add(_filledPhrases[idx]);
        }

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.RegisterStory(
                filledBlocks.ToArray(),
                _totalFactionA,
                _totalFactionB
            );

        TutorialManager.Instance?.OnTutorialEvent(TutorialEventType.StorySubmitted);

        _onSubmitted?.Invoke(_currentTopic);
    }
}
