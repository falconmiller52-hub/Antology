using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Визуальная нить между двумя ячейками на карте.
/// Прямая линия через Image, растянутый и повёрнутый.
/// ПКМ по нити — удалить связь.
/// Цвет:
///  - Голубой: связь разрешена и пока не завершающая.
///  - Красный: связь недопустимая (категории не совпадают, не 0→1→2→3).
///    Для прототипа: недопустимые связи не создаются. Красный зарезервирован.
///  - Зелёный: цепочка полностью собрана (0→1→2→3) — окрашиваются все нити цепочки.
///
/// Настройка префаба:
/// 1. UI Image, Anchor = middle-left, Pivot = (0, 0.5).
/// 2. Raycast Target = ON для перехвата ПКМ.
/// 3. Повесьте этот скрипт.
/// </summary>
public class StoryConnectionUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual")]
    [SerializeField] private Image lineImage;
    [SerializeField] private float lineThickness = 4f;

    [Header("Colors")]
    [SerializeField] private Color validColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color completeColor = new Color(0.3f, 1f, 0.4f, 1f);

    public StoryNodeUI From { get; private set; }
    public StoryNodeUI To { get; private set; }

    private RectTransform _rect;
    private StoryMapUI _map;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (lineImage == null)
            lineImage = GetComponent<Image>();
    }

    public void Initialize(StoryNodeUI from, StoryNodeUI to, StoryMapUI map)
    {
        From = from;
        To = to;
        _map = map;
        UpdateShape();
        SetState(ConnectionState.Valid);
    }

    /// <summary>
    /// Пересчитать позицию/длину/угол линии между двумя сокетами.
    /// Вызывается при перемещении любой из ячеек.
    /// </summary>
    public void UpdateShape()
    {
        if (From == null || To == null) return;

        Vector2 a = From.GetSocketLocalPosition(SocketType.Output);
        Vector2 b = To.GetSocketLocalPosition(SocketType.Input);

        Vector2 diff = b - a;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        _rect.localPosition = a;
        _rect.sizeDelta = new Vector2(length, lineThickness);
        _rect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetState(ConnectionState state)
    {
        if (lineImage == null) return;

        switch (state)
        {
            case ConnectionState.Valid:   lineImage.color = validColor;    break;
            case ConnectionState.Invalid: lineImage.color = invalidColor;  break;
            case ConnectionState.Complete:lineImage.color = completeColor; break;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _map?.RemoveConnection(this);
        }
    }

    public enum ConnectionState { Valid, Invalid, Complete }
}
