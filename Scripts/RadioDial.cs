using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Вращаемая ручка настройки частоты.
/// Игрок зажимает ЛКМ на ручке и двигает мышь вправо/влево — ручка вращается
/// синхронно с движением мыши вокруг оси Z.
///
/// Реализация через абсолютную позицию мыши между кадрами (а не Mouse.delta),
/// благодаря чему движение плавное и не зависит от fps.
///
/// Настройка:
/// 1. Дочерний GameObject от Radio со SpriteRenderer (спрайт ручки).
/// 2. Добавьте CircleCollider2D или BoxCollider2D.
/// 3. Повесьте этот скрипт.
/// </summary>
public class RadioDial : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Минимальный угол поворота (градусы)")]
    [SerializeField] private float minAngle = -150f;
    [Tooltip("Максимальный угол поворота (градусы)")]
    [SerializeField] private float maxAngle = 150f;
    [Tooltip("Градусов поворота на 1 юнит горизонтального движения мыши в мире. " +
             "Увеличьте для более быстрой реакции, уменьшите для точности.")]
    [SerializeField] private float degreesPerWorldUnit = 180f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer dialRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite highlightSprite;

    /// <summary>
    /// Нормализованное значение от 0 до 1.
    /// 0 = minAngle, 1 = maxAngle.
    /// </summary>
    public float NormalizedValue { get; private set; } = 0.5f;

    private bool _isInteractable = true;
    private bool _isDragging;
    private bool _isHovered;
    private float _currentAngle;

    // Отслеживание позиции мыши между кадрами в мировых координатах.
    // Использование абсолютной позиции (а не Mouse.delta) даёт плавное
    // движение без дёрганий, синхронное с движением курсора.
    private float _lastMouseWorldX;

    private Camera _mainCamera;
    private CustomCursor _customCursor;

    private void Awake()
    {
        if (dialRenderer == null)
            dialRenderer = GetComponent<SpriteRenderer>();

        _mainCamera = Camera.main;
        _customCursor = FindFirstObjectByType<CustomCursor>();

        // Начинаем с середины
        _currentAngle = Mathf.Lerp(minAngle, maxAngle, 0.5f);
        ApplyRotation();
    }

    private void Update()
    {
        if (!_isInteractable || Mouse.current == null) return;

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bool hoveringThis = IsMouseOverThis(mouseWorldPos);

        // Hover
        if (hoveringThis && !_isHovered)
        {
            _isHovered = true;
            if (highlightSprite != null)
                dialRenderer.sprite = highlightSprite;
            _customCursor?.SetHover();
        }
        else if (!hoveringThis && _isHovered && !_isDragging)
        {
            _isHovered = false;
            if (normalSprite != null)
                dialRenderer.sprite = normalSprite;
            _customCursor?.SetDefault();
        }

        // Начало вращения
        if (hoveringThis && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _isDragging = true;
            _lastMouseWorldX = mouseWorldPos.x;
            _customCursor?.SetGrab();
            AudioManager.Instance?.PlayRadioTuning();
        }

        // Процесс вращения
        if (_isDragging)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                // Разница между текущей и прошлой позицией мыши в мировых единицах
                // по горизонтали. Это даёт плавный, синхронный с мышью поворот:
                // мышь сдвинулась — ручка повернулась на столько же (× множитель).
                float deltaX = mouseWorldPos.x - _lastMouseWorldX;
                float rotationDelta = deltaX * degreesPerWorldUnit;

                _currentAngle = Mathf.Clamp(_currentAngle + rotationDelta, minAngle, maxAngle);
                ApplyRotation();
                UpdateNormalizedValue();

                _lastMouseWorldX = mouseWorldPos.x;
            }
            else
            {
                _isDragging = false;
                _customCursor?.SetDefault();
                if (_isHovered) _customCursor?.SetHover();
            }
        }
    }

    private void ApplyRotation()
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
    }

    private void UpdateNormalizedValue()
    {
        NormalizedValue = Mathf.InverseLerp(minAngle, maxAngle, _currentAngle);
    }

    /// <summary>
    /// Включает/отключает возможность взаимодействия с ручкой.
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        Debug.Log($"[RadioDial] {gameObject.name} interactable = {interactable}");

        if (!interactable && _isDragging)
        {
            _isDragging = false;
            _customCursor?.SetDefault();
        }

        // Визуальная обратная связь — приглушаем неактивные ручки
        if (dialRenderer != null)
        {
            Color c = dialRenderer.color;
            c.a = interactable ? 1f : 0.5f;
            dialRenderer.color = c;
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
