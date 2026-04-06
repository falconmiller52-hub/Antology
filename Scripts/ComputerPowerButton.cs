using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Кнопка включения компьютера. Кликабельный спрайт в мире.
/// При нажатии открывает ComputerManager (UI каталога тем).
///
/// Настройка:
/// 1. Дочерний GameObject от PC со SpriteRenderer + BoxCollider2D.
/// 2. Назначьте computerManager в инспекторе.
/// </summary>
public class ComputerPowerButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComputerManager computerManager;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;

    private SpriteRenderer _renderer;
    private Camera _mainCamera;
    private CustomCursor _customCursor;
    private bool _isHovered;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;
        _customCursor = FindFirstObjectByType<CustomCursor>();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bool hoveringThis = IsMouseOverThis(mouseWorldPos);

        if (hoveringThis && !_isHovered)
        {
            _isHovered = true;
            if (hoverSprite != null) _renderer.sprite = hoverSprite;
            _customCursor?.SetHover();
        }
        else if (!hoveringThis && _isHovered)
        {
            _isHovered = false;
            if (normalSprite != null) _renderer.sprite = normalSprite;
            _customCursor?.SetDefault();
        }

        if (hoveringThis && Mouse.current.leftButton.wasPressedThisFrame)
        {
            AudioManager.Instance?.PlayPCPower();
            computerManager?.OpenComputer();
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
