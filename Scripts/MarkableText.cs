using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Интерактивный текстовый блок в меню газеты/письма.
/// Игрок может навести (смена цвета) и кликнуть (маркирование — собирает IntelKey).
///
/// Настройка:
/// 1. На TextMeshProUGUI внутри menuPanel газеты/письма.
/// 2. Назначьте intelKey — ключ, который собирается при маркировании.
/// 3. Если intelKey = null, блок обычный (не интерактивный).
/// </summary>
public class MarkableText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Intel")]
    [Tooltip("Ключ, собираемый при маркировании. Null = обычный текст.")]
    [SerializeField] private IntelKey intelKey;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color hoverColor = new Color(0.4f, 0.4f, 0.15f, 1f);
    [SerializeField] private Color markedColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private TextMeshProUGUI _text;
    private bool _isMarked;
    private bool _isInteractive;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _isInteractive = intelKey != null;

        // Проверяем, не был ли уже собран ключ ранее
        if (_isInteractive && IntelManager.Instance != null && IntelManager.Instance.HasKey(intelKey))
        {
            _isMarked = true;
            _text.color = markedColor;
        }
        else
        {
            _text.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isInteractive || _isMarked) return;
        _text.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isInteractive || _isMarked) return;
        _text.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isInteractive || _isMarked) return;

        _isMarked = true;
        _text.color = markedColor;

        IntelManager.Instance?.CollectKey(intelKey);
    }
}
