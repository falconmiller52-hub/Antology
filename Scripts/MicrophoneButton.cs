using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Интерактивный спрайт микрофона на рабочем столе.
/// Имеет два состояния: неактивирован (с наушниками) и активирован (без наушников).
/// В каждом состоянии — обычный спрайт и спрайт с обводкой (при наведении).
/// При активации дочерний спрайт наушников скрывается.
///
/// Настройка:
/// 1. GameObject со SpriteRenderer + BoxCollider2D + этот скрипт.
/// 2. Дочерний GameObject "Headphones" со SpriteRenderer.
/// 3. Назначьте все 4 спрайта и headphonesObject в инспекторе.
/// </summary>
public class MicrophoneButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InterviewManager interviewManager;
    [SerializeField] private GameObject headphonesObject;

    [Header("Inactive Sprites (с наушниками)")]
    [SerializeField] private Sprite inactiveNormal;
    [SerializeField] private Sprite inactiveOutline;

    [Header("Active Sprites (без наушников)")]
    [SerializeField] private Sprite activeNormal;
    [SerializeField] private Sprite activeOutline;

    private SpriteRenderer _renderer;
    private Camera _mainCamera;
    private CustomCursor _customCursor;
    private bool _isHovered;
    private bool _isActive;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;
        _customCursor = FindFirstObjectByType<CustomCursor>();

        SetActiveState(false);
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bool hoveringThis = IsMouseOverThis(mouseWorldPos);

        if (hoveringThis && !_isHovered)
        {
            _isHovered = true;
            _renderer.sprite = _isActive ? activeOutline : inactiveOutline;
            _customCursor?.SetHover();
        }
        else if (!hoveringThis && _isHovered)
        {
            _isHovered = false;
            _renderer.sprite = _isActive ? activeNormal : inactiveNormal;
            _customCursor?.SetDefault();
        }

        if (hoveringThis && Mouse.current.leftButton.wasPressedThisFrame)
        {
            AudioManager.Instance?.PlayEquipmentActivate();

            _isActive = !_isActive;
            SetActiveState(_isActive);

            if (_isActive)
                interviewManager?.OpenCatalog();
        }
    }

    /// <summary>
    /// Вызывается из InterviewManager когда интервью закрывается.
    /// </summary>
    public void Deactivate()
    {
        SetActiveState(false);
    }

    private void SetActiveState(bool active)
    {
        _isActive = active;

        if (headphonesObject != null)
            headphonesObject.SetActive(!active);

        if (_isHovered)
            _renderer.sprite = active ? activeOutline : inactiveOutline;
        else
            _renderer.sprite = active ? activeNormal : inactiveNormal;
    }

    private bool IsMouseOverThis(Vector2 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        foreach (var hit in hits)
            if (hit.gameObject == gameObject) return true;
        return false;
    }
}
