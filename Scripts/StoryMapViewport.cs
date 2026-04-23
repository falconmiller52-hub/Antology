using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Контроллер вьюпорта ментальной карты: панорамирование (ЛКМ-драг по пустоте)
/// и зум (колесо мыши с центром в позиции курсора).
///
/// Также ловит ЛКМ-клики в "пустое место" (через IPointerDownHandler) и уведомляет
/// StoryMapUI, чтобы тот скрыл боковую панель. Ноды и сокеты перехватывают
/// клики первыми через свои IPointerDownHandler, поэтому сюда доходят только
/// клики "мимо всех нод/сокетов".
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StoryMapViewport : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    [Header("Notify")]
    [Tooltip("Если назначен — при клике в пустое место будет вызван OnEmptySpaceClicked.")]
    [SerializeField] private StoryMapUI mapUI;
    [Header("Zoom")]
    [Tooltip("Множитель на одно деление колеса. 0.1 = +10% за шаг.")]
    [SerializeField] private float zoomStep = 0.1f;
    [SerializeField] private float minZoom = 0.3f;
    [SerializeField] private float maxZoom = 2.5f;

    [Header("Pan Bounds")]
    [Tooltip("Размер холста в локальных (не-зумленых) единицах. " +
             "Если 0 — берётся sizeDelta самой mapArea. Если mapArea больше родителя, " +
             "pan ограничен, чтобы края холста не заходили за край родителя.")]
    [SerializeField] private Vector2 canvasSize = Vector2.zero;

    [Tooltip("Если true — значения canvasSize и zoom применяются в Start() к mapArea.")]
    [SerializeField] private bool applySizeOnStart = true;

    private RectTransform _rect;
    private RectTransform _parentRect;
    private bool _isPanning;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _parentRect = _rect.parent as RectTransform;

        if (applySizeOnStart && canvasSize != Vector2.zero)
            _rect.sizeDelta = canvasSize;
    }

    private void Start()
    {
        // Применяем начальный clamp — на случай если в редакторе руками сдвинули
        ClampPosition();
    }

    /// <summary>
    /// Позволяет извне задать размер холста (например, из StoryMapUI на основе данных темы).
    /// </summary>
    public void SetCanvasSize(Vector2 size)
    {
        canvasSize = size;
        if (_rect != null) _rect.sizeDelta = size;
        ClampPosition();
    }

    public void ResetView()
    {
        _rect.localScale = Vector3.one;
        _rect.localPosition = Vector3.zero;
        ClampPosition();
    }

    // ===== Pan =====

    public void OnPointerDown(PointerEventData eventData)
    {
        // Клик по пустому месту (до нод/сокетов дошло) → скрываем боковую панель.
        if (eventData.button == PointerEventData.InputButton.Left && mapUI != null)
            mapUI.OnEmptySpaceClicked();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _isPanning = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isPanning) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // eventData.delta — движение курсора в screen-пикселях за кадр.
        // Канвертируем в локальные единицы родителя (с учётом масштаба canvas).
        Vector2 worldDelta = ScreenDeltaToLocalDelta(eventData.delta, eventData.pressEventCamera);
        _rect.localPosition += (Vector3)worldDelta;
        ClampPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isPanning = false;
    }

    // ===== Zoom =====

    public void OnScroll(PointerEventData eventData)
    {
        float scrollY = eventData.scrollDelta.y;
        if (Mathf.Approximately(scrollY, 0f)) return;

        // Шаг зависит от направления и величины. scrollDelta обычно ±1 на одно деление,
        // но на трекпадах может быть дробным — используем как множитель.
        float factor = 1f + zoomStep * Mathf.Sign(scrollY);

        float currentZoom = _rect.localScale.x;
        float newZoom = Mathf.Clamp(currentZoom * factor, minZoom, maxZoom);

        if (Mathf.Approximately(newZoom, currentZoom)) return;

        // Зум к позиции курсора: точка под курсором должна остаться под курсором.
        // 1. Получаем позицию курсора в локальных координатах mapArea ДО зума.
        Vector2 localBeforeZoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect, eventData.position, eventData.pressEventCamera, out localBeforeZoom);

        // 2. Применяем зум.
        float zoomRatio = newZoom / currentZoom;
        _rect.localScale = Vector3.one * newZoom;

        // 3. После скейла локальная точка курсора "сжалась/растянулась" относительно pivot.
        //    Нам нужно сдвинуть rect так, чтобы эта локальная точка в родительских
        //    координатах была ровно под курсором.
        //    Проще: сдвигаем localPosition на разницу (screenBefore - screenAfter) в родителе.
        Vector2 localAfterZoom = localBeforeZoom * zoomRatio;
        Vector2 diffLocal = localBeforeZoom - localAfterZoom; // в локальных единицах mapArea
        // Но сдвигать нужно в родительских единицах — а это те же локальные mapArea,
        // умноженные на новый scale.
        Vector3 parentShift = (Vector3)diffLocal * newZoom;
        _rect.localPosition += parentShift;

        ClampPosition();
    }

    // ===== Границы =====

    /// <summary>
    /// Фиксирует localPosition так, чтобы края mapArea (с учётом масштаба) не заходили
    /// внутрь от края родителя. Если mapArea меньше родителя — центрируем.
    /// </summary>
    private void ClampPosition()
    {
        if (_parentRect == null) return;

        Vector2 mapSize = _rect.sizeDelta * _rect.localScale.x;
        Vector2 parentSize = _parentRect.rect.size;

        Vector3 pos = _rect.localPosition;

        // По X
        if (mapSize.x <= parentSize.x)
        {
            // Холст уже влезает — центрируем
            pos.x = 0f;
        }
        else
        {
            float halfExtraX = (mapSize.x - parentSize.x) * 0.5f;
            pos.x = Mathf.Clamp(pos.x, -halfExtraX, halfExtraX);
        }

        // По Y
        if (mapSize.y <= parentSize.y)
        {
            pos.y = 0f;
        }
        else
        {
            float halfExtraY = (mapSize.y - parentSize.y) * 0.5f;
            pos.y = Mathf.Clamp(pos.y, -halfExtraY, halfExtraY);
        }

        _rect.localPosition = pos;
    }

    // ===== Helpers =====

    /// <summary>
    /// Конвертирует screen-delta в локальные единицы родителя (для pan).
    /// </summary>
    private Vector2 ScreenDeltaToLocalDelta(Vector2 screenDelta, Camera cam)
    {
        if (_parentRect == null) return screenDelta;

        Vector2 p1, p2;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, Vector2.zero, cam, out p1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenDelta, cam, out p2);
        return p2 - p1;
    }
}
