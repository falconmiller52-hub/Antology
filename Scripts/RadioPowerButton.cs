using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Кнопка включения/выключения радиоприёмника.
/// Кликабельный спрайт, который вызывает RadioReceiver.TogglePower().
///
/// Настройка:
/// 1. Дочерний GameObject от Radio со SpriteRenderer.
/// 2. Добавьте BoxCollider2D.
/// 3. Назначьте radioReceiver в инспекторе.
/// </summary>
public class RadioPowerButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RadioReceiver radioReceiver;

    [Header("Sprites")]
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite hoverSprite;

    private SpriteRenderer _renderer;
    private Camera _mainCamera;
    private CustomCursor _customCursor;
    private bool _isOn;
    private bool _isHovered;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;
        _customCursor = FindFirstObjectByType<CustomCursor>();

        if (offSprite != null)
            _renderer.sprite = offSprite;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bool hoveringThis = IsMouseOverThis(mouseWorldPos);

        // Hover
        if (hoveringThis && !_isHovered)
        {
            _isHovered = true;
            if (hoverSprite != null)
                _renderer.sprite = hoverSprite;
            _customCursor?.SetHover();
        }
        else if (!hoveringThis && _isHovered)
        {
            _isHovered = false;
            _renderer.sprite = _isOn ? onSprite : offSprite;
            _customCursor?.SetDefault();
        }

        // Клик
        if (hoveringThis && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _isOn = !_isOn;
            _renderer.sprite = _isOn ? onSprite : offSprite;

            AudioManager.Instance?.PlayEquipmentActivate();

            if (radioReceiver != null)
            {
                radioReceiver.TogglePower();
            }
            else
            {
                Debug.LogWarning("[RadioPowerButton] radioReceiver is NULL! Assign it in Inspector.");
            }
        }
    }

    private bool IsMouseOverThis(Vector2 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) return true;
        }
        return false;
    }
}
