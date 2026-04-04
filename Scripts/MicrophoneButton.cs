using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Интерактивный спрайт микрофона на рабочем столе.
/// При клике открывает каталог персоналий для интервью.
///
/// Настройка:
/// 1. GameObject со SpriteRenderer + BoxCollider2D.
/// 2. Назначьте interviewManager в инспекторе.
/// </summary>
public class MicrophoneButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InterviewManager interviewManager;

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
            AudioManager.Instance?.PlayButtonClick();
            interviewManager?.OpenCatalog();
        }
    }

    private bool IsMouseOverThis(Vector2 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        foreach (var hit in hits)
            if (hit.gameObject == gameObject) return true;
        return false;
    }
}
