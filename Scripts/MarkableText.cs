using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Интерактивный текстовый блок в меню газеты/письма.
/// Работает через EventSystem (IPointer) И через ручную проверку как fallback.
///
/// Требования:
/// 1. Canvas с Graphic Raycaster.
/// 2. EventSystem с Input System UI Input Module.
/// 3. На TextMeshProUGUI: Raycast Target = true.
/// </summary>
public class MarkableText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Intel")]
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

        if (_isInteractive && IntelManager.Instance != null && IntelManager.Instance.HasKey(intelKey))
        {
            _isMarked = true;
            _text.color = markedColor;
        }
        else
        {
            _text.color = _isInteractive ? normalColor : _text.color;
        }

        // Убедимся что Raycast Target включён для интерактивных
        if (_isInteractive)
            _text.raycastTarget = true;
    }

    private void Start()
    {
        if (_isInteractive)
        {
            Debug.Log($"[MarkableText] '{gameObject.name}' interactive, intelKey='{intelKey.keyName}', marked={_isMarked}");
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

        Debug.Log($"[MarkableText] Clicked! Marking '{intelKey.keyName}'");

        _isMarked = true;
        _text.color = markedColor;

        if (IntelManager.Instance != null)
        {
            IntelManager.Instance.CollectKey(intelKey);
        }
        else
        {
            Debug.LogError("[MarkableText] IntelManager.Instance is NULL! Add IntelManager to MainMenu scene or Gameplay scene.");
            // Прямой вызов туториала как fallback
            TutorialManager.Instance?.OnTutorialEvent(TutorialEventType.IntelCollected);
        }
    }
}
