using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Базовый класс для интерактивных предметов (газеты, письма).
/// Обеспечивает: смену спрайта при наведении, подъём, перетаскивание ЛКМ, открытие меню.
/// 
/// Настройка:
/// 1. На GameObject со SpriteRenderer повесьте этот скрипт (или наследника).
/// 2. Добавьте Collider2D (BoxCollider2D) для обнаружения мыши.
/// 3. Назначьте спрайты и menuPanel в инспекторе.
/// 4. menuPanel — уникальная менюшка этого предмета (деактивированный GameObject).
/// 5. На главной камере должен быть Physics2DRaycaster (для мировых объектов)
///    или убедитесь, что камера помечена MainCamera.
/// </summary>
public class InteractableItem : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] protected Sprite normalSprite;
    [SerializeField] protected Sprite outlineSprite;

    [Header("Hover Animation")]
    [SerializeField] private float hoverLiftAmount = 0.15f;
    [SerializeField] private float hoverSpeed = 8f;

    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;

    [Header("Drag Settings")]
    [SerializeField] private float dragSmoothing = 15f;
    [SerializeField] private float clickThreshold = 0.15f;

    /// <summary>
    /// Статический флаг: открыто ли сейчас любое меню предмета.
    /// GameplayManager проверяет его, чтобы не открывать паузу поверх меню.
    /// </summary>
    public static bool AnyMenuOpen { get; private set; }

    protected SpriteRenderer spriteRenderer;
    protected bool isOpened;
    private bool _isHovered;
    private bool _isDragging;
    private bool _menuIsOpen;

    private Vector3 _originalPosition;
    private Vector3 _targetPosition;
    private Vector3 _dragOffset;
    private Vector2 _dragStartMousePos;

    private Camera _mainCamera;
    private CustomCursor _customCursor;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;
        _customCursor = FindFirstObjectByType<CustomCursor>();

        _originalPosition = transform.position;
        _targetPosition = _originalPosition;

        if (normalSprite != null)
            spriteRenderer.sprite = normalSprite;

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    protected virtual void Update()
    {
        HandleInput();
        UpdatePosition();
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        // Закрытие меню по Esc или клику мыши
        if (_menuIsOpen)
        {
            bool escPressed = Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame;
            bool clickedAnywhere = Mouse.current.leftButton.wasPressedThisFrame;

            if (escPressed || clickedAnywhere)
            {
                CloseMenu();
                return;
            }

            // Пока меню открыто — не обрабатываем hover/drag
            return;
        }

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // Рейкаст для определения наведения
        bool hoveringThis = IsMouseOverThis(mouseWorldPos);

        // Hover enter/exit
        if (hoveringThis && !_isHovered && !_isDragging)
            OnHoverEnter();
        else if (!hoveringThis && _isHovered && !_isDragging)
            OnHoverExit();

        // Начало нажатия — запоминаем стартовую позицию мыши
        if (hoveringThis && Mouse.current.leftButton.wasPressedThisFrame && !_menuIsOpen)
        {
            _isDragging = true;
            _dragStartMousePos = mouseWorldPos;
            _dragOffset = transform.position - (Vector3)mouseWorldPos;
            _customCursor?.SetGrab();
        }

        if (_isDragging)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                _targetPosition = (Vector3)mouseWorldPos + _dragOffset;
                _targetPosition.y += hoverLiftAmount;
            }
            else
            {
                // Отпустили кнопку — определяем: клик или перетаскивание?
                _isDragging = false;
                float dragDistance = Vector2.Distance(_dragStartMousePos, mouseWorldPos);

                if (dragDistance < clickThreshold)
                {
                    // Это клик — открываем меню, возвращаем на место
                    _targetPosition = _originalPosition + (_isHovered ? Vector3.up * hoverLiftAmount : Vector3.zero);
                    OpenMenu();
                }
                else
                {
                    // Это перетаскивание — фиксируем новую позицию
                    _originalPosition = new Vector3(
                        transform.position.x,
                        transform.position.y - hoverLiftAmount,
                        transform.position.z
                    );
                    _targetPosition = _originalPosition + (_isHovered ? Vector3.up * hoverLiftAmount : Vector3.zero);
                }

                _customCursor?.SetDefault();
                if (_isHovered) _customCursor?.SetHover();
            }
        }
    }

    /// <summary>
    /// Вызывается при наведении курсора на предмет.
    /// </summary>
    private void OnHoverEnter()
    {
        _isHovered = true;

        // Меняем спрайт на версию с обводкой
        Sprite hover = GetHoverSprite();
        if (hover != null)
            spriteRenderer.sprite = hover;

        // Поднимаем вверх
        _targetPosition = (_isDragging ? _targetPosition : _originalPosition) + Vector3.up * hoverLiftAmount;

        if (!_isDragging)
            _customCursor?.SetHover();
    }

    /// <summary>
    /// Вызывается когда курсор уходит с предмета.
    /// </summary>
    private void OnHoverExit()
    {
        _isHovered = false;

        // Возвращаем обычный спрайт
        Sprite normal = GetNormalSprite();
        if (normal != null)
            spriteRenderer.sprite = normal;

        // Опускаем обратно
        _targetPosition = _originalPosition;

        _customCursor?.SetDefault();
    }

    private void UpdatePosition()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            _targetPosition,
            Time.deltaTime * hoverSpeed
        );
    }

    /// <summary>
    /// Возвращает спрайт для состояния наведения.
    /// Переопределяется в наследниках для учёта состояния "открыто".
    /// </summary>
    protected virtual Sprite GetHoverSprite()
    {
        return outlineSprite;
    }

    /// <summary>
    /// Возвращает обычный спрайт.
    /// Переопределяется в наследниках для учёта состояния "открыто".
    /// </summary>
    protected virtual Sprite GetNormalSprite()
    {
        return normalSprite;
    }

    /// <summary>
    /// Открывает уникальное меню этого предмета.
    /// Вызывается при клике ЛКМ.
    /// </summary>
    public void OpenMenu()
    {
        if (menuPanel == null) return;

        _menuIsOpen = true;
        AnyMenuOpen = true;
        menuPanel.SetActive(true);

        AudioManager.Instance?.PlayButtonClick();
        OnMenuOpened();
    }

    /// <summary>
    /// Закрывает меню предмета. Вызывается по Esc или клику мыши.
    /// </summary>
    public void CloseMenu()
    {
        if (menuPanel == null) return;

        _menuIsOpen = false;
        AnyMenuOpen = false;
        menuPanel.SetActive(false);

        AudioManager.Instance?.PlayButtonClick();
    }

    /// <summary>
    /// Вызывается после открытия меню.
    /// Переопределяется в наследниках для смены спрайта (например, у писем).
    /// </summary>
    protected virtual void OnMenuOpened()
    {
        isOpened = true;
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
