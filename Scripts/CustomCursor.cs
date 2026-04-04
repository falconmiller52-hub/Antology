using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Кастомный курсор, отображаемый поверх всего UI.
/// 
/// Настройка:
/// 1. Создайте Canvas с Render Mode = Screen Space - Overlay, Sort Order = 999.
/// 2. Добавьте на Canvas дочерний GameObject "CursorSprite" с компонентом Image.
/// 3. Повесьте этот скрипт на "CursorSprite".
/// 4. Назначьте cursorImage в инспекторе (или скрипт найдёт сам).
/// 5. В Image назначьте спрайт курсора и поставьте Raycast Target = OFF.
/// </summary>
public class CustomCursor : MonoBehaviour
{
    [Header("Cursor Sprites")]
    [SerializeField] private Sprite defaultCursor;
    [SerializeField] private Sprite hoverCursor;
    [SerializeField] private Sprite grabCursor;

    [Header("References")]
    [SerializeField] private Image cursorImage;

    [Header("Settings")]
    [Tooltip("Смещение от позиции мыши (в пикселях), чтобы остриё попадало точно")]
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    private RectTransform _rectTransform;
    private Canvas _parentCanvas;

    private void Awake()
    {
        if (cursorImage == null)
            cursorImage = GetComponent<Image>();

        _rectTransform = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();

        // Отключаем системный курсор
        UnityEngine.Cursor.visible = false;

        // Убеждаемся, что Image не блокирует рейкасты
        cursorImage.raycastTarget = false;

        SetDefault();
    }

    private void Update()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (_parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _rectTransform.position = (Vector3)(mousePos + hotspot);
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentCanvas.transform as RectTransform,
                mousePos,
                _parentCanvas.worldCamera,
                out Vector2 localPoint
            );
            _rectTransform.localPosition = localPoint + hotspot;
        }
    }

    /// <summary>
    /// Обычный курсор.
    /// </summary>
    public void SetDefault()
    {
        if (defaultCursor != null)
            cursorImage.sprite = defaultCursor;
    }

    /// <summary>
    /// Курсор при наведении на интерактивный объект.
    /// </summary>
    public void SetHover()
    {
        if (hoverCursor != null)
            cursorImage.sprite = hoverCursor;
        else
            SetDefault();
    }

    /// <summary>
    /// Курсор при перетаскивании.
    /// </summary>
    public void SetGrab()
    {
        if (grabCursor != null)
            cursorImage.sprite = grabCursor;
        else
            SetHover();
    }

    private void OnDisable()
    {
        UnityEngine.Cursor.visible = true;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        UnityEngine.Cursor.visible = !hasFocus;
    }
}
