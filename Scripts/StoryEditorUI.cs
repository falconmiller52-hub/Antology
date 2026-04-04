using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Экран редактора сюжета. Разделён на две части:
/// - Левая: блоки-пропуски, заполняемые по порядку.
/// - Правая: варианты фраз для текущего блока.
/// После заполнения всех блоков появляется кнопка "Отправить".
///
/// Настройка:
/// 1. На StoryEditorPanel повесьте этот скрипт.
/// 2. Левая панель: Vertical Layout Group контейнер (blocksContainer).
/// 3. Правая панель: Vertical Layout Group контейнер (phrasesContainer).
/// 4. Создайте два префаба:
///    - blockSlotPrefab: UI элемент блока (Image фон + TextMeshProUGUI).
///    - phraseButtonPrefab: Button с TextMeshProUGUI.
/// 5. Кнопка submitButton — деактивирована по умолчанию.
/// 6. topicTitleText — заголовок темы вверху редактора.
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

    [Header("Colors")]
    [SerializeField] private Color emptyBlockColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color activeBlockColor = new Color(0.4f, 0.6f, 0.4f, 1f);
    [SerializeField] private Color filledBlockColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);

    private StoryTopic _currentTopic;
    private System.Action<StoryTopic> _onSubmitted;

    private int _currentBlockIndex;
    private string[] _filledPhrases;
    private GameObject[] _blockSlots;

    /// <summary>
    /// Инициализирует редактор для заданной темы.
    /// </summary>
    public void Initialize(StoryTopic topic, System.Action<StoryTopic> onSubmitted)
    {
        _currentTopic = topic;
        _onSubmitted = onSubmitted;
        _currentBlockIndex = 0;
        _filledPhrases = new string[topic.blocks.Length];
        _blockSlots = new GameObject[topic.blocks.Length];

        // Заголовок
        if (topicTitleText != null)
            topicTitleText.text = topic.topicTitle;

        // Кнопка отправки
        submitButton.gameObject.SetActive(false);
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(OnSubmit);

        BuildBlocks();
        ShowPhrasesForCurrentBlock();
    }

    private void BuildBlocks()
    {
        // Очищаем
        foreach (Transform child in blocksContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < _currentTopic.blocks.Length; i++)
        {
            GameObject slot = Instantiate(blockSlotPrefab, blocksContainer);
            _blockSlots[i] = slot;

            TextMeshProUGUI slotText = slot.GetComponentInChildren<TextMeshProUGUI>();
            Image slotBg = slot.GetComponent<Image>();

            if (slotText != null)
                slotText.text = _currentTopic.blocks[i].blockLabel + ": ___";

            if (slotBg != null)
                slotBg.color = (i == 0) ? activeBlockColor : emptyBlockColor;
        }
    }

    private void ShowPhrasesForCurrentBlock()
    {
        // Очищаем правую панель
        foreach (Transform child in phrasesContainer)
            Destroy(child.gameObject);

        if (_currentBlockIndex >= _currentTopic.blocks.Length) return;

        StoryBlock block = _currentTopic.blocks[_currentBlockIndex];

        for (int i = 0; i < block.phraseOptions.Length; i++)
        {
            GameObject btnObj = Instantiate(phraseButtonPrefab, phrasesContainer);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = block.phraseOptions[i];

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string capturedPhrase = block.phraseOptions[i];
                btn.onClick.AddListener(() => OnPhraseSelected(capturedPhrase));
            }
        }
    }

    private void OnPhraseSelected(string phrase)
    {
        AudioManager.Instance?.PlayButtonClick();

        // Заполняем текущий блок
        _filledPhrases[_currentBlockIndex] = phrase;

        // Обновляем визуал заполненного блока
        UpdateBlockSlot(_currentBlockIndex, phrase);

        // Переходим к следующему блоку
        _currentBlockIndex++;

        if (_currentBlockIndex >= _currentTopic.blocks.Length)
        {
            // Все блоки заполнены — показываем кнопку "Отправить"
            ClearPhrases();
            submitButton.gameObject.SetActive(true);
        }
        else
        {
            // Подсвечиваем следующий блок
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
            slotText.text = _currentTopic.blocks[index].blockLabel + ": " + phrase;

        if (slotBg != null)
            slotBg.color = filledBlockColor;
    }

    private void HighlightCurrentBlock()
    {
        for (int i = 0; i < _blockSlots.Length; i++)
        {
            if (_blockSlots[i] == null) continue;

            Image bg = _blockSlots[i].GetComponent<Image>();
            if (bg == null) continue;

            if (i == _currentBlockIndex)
                bg.color = activeBlockColor;
            else if (i < _currentBlockIndex)
                bg.color = filledBlockColor;
            else
                bg.color = emptyBlockColor;
        }
    }

    private void ClearPhrases()
    {
        foreach (Transform child in phrasesContainer)
            Destroy(child.gameObject);
    }

    private void OnSubmit()
    {
        _onSubmitted?.Invoke(_currentTopic);
    }
}
