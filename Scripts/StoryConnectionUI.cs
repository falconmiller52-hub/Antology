using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Визуальная нить между двумя ячейками на карте.
///
/// ВАЖНО: независимо от настроек префаба, в Awake принудительно выставляются:
///  - Anchor Min = Max = (0, 0.5)
///  - Pivot = (0, 0.5)
///  - Scale = (1, 1, 1)
/// Это гарантирует, что sizeDelta.x работает как реальная длина линии,
/// а sizeDelta.y — как реальная толщина (а не смещение от растянутого anchor).
///
/// Толщина настраивается в поле lineThickness.
/// НЕ используйте масштабирование префаба для изменения толщины.
/// </summary>
public class StoryConnectionUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual")]
    [SerializeField] private Image lineImage;
    [Tooltip("Толщина линии в пикселях Canvas'а. НЕ используйте scale префаба для изменения толщины.")]
    [SerializeField] private float lineThickness = 4f;

    [Header("Colors")]
    [SerializeField] private Color validColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color completeColor = new Color(0.3f, 1f, 0.4f, 1f);
    [SerializeField] private Color draggingColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);

    public StoryNodeUI From { get; private set; }
    public StoryNodeUI To { get; private set; }
    public SocketType FromSocketType { get; private set; }

    private bool _isDragging;
    private Vector2 _draggingEndpoint;

    private RectTransform _rect;
    private StoryMapUI _map;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (lineImage == null)
            lineImage = GetComponent<Image>();

        // Принудительный сброс — защита от неправильно собранного префаба.
        _rect.anchorMin = new Vector2(0f, 0.5f);
        _rect.anchorMax = new Vector2(0f, 0.5f);
        _rect.pivot = new Vector2(0f, 0.5f);
        _rect.localScale = Vector3.one;
        _rect.sizeDelta = new Vector2(0f, lineThickness);
    }

    public void InitializeComplete(StoryNodeUI from, StoryNodeUI to, StoryMapUI map)
    {
        From = from;
        To = to;
        FromSocketType = SocketType.Output;
        _isDragging = false;
        _map = map;
        if (lineImage != null) lineImage.raycastTarget = true;
        UpdateShape();
        SetState(ConnectionState.Valid);
    }

    public void InitializeDragging(StoryNodeUI from, SocketType fromSocket, StoryMapUI map)
    {
        From = from;
        To = null;
        FromSocketType = fromSocket;
        _isDragging = true;
        _draggingEndpoint = from.GetSocketLocalPosition(fromSocket);
        _map = map;
        if (lineImage != null) lineImage.raycastTarget = false;
        SetState(ConnectionState.Dragging);
        UpdateShape();
    }

    public void UpdateDraggingEndpoint(Vector2 localPoint)
    {
        _draggingEndpoint = localPoint;
        UpdateShape();
    }

    public void UpdateShape()
    {
        if (_rect == null) return;

        Vector2 a, b;
        if (_isDragging)
        {
            if (From == null) return;
            a = From.GetSocketLocalPosition(FromSocketType);
            b = _draggingEndpoint;
        }
        else
        {
            if (From == null || To == null) return;
            a = From.GetSocketLocalPosition(SocketType.Output);
            b = To.GetSocketLocalPosition(SocketType.Input);
        }

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
            case ConnectionState.Valid:    lineImage.color = validColor;    break;
            case ConnectionState.Invalid:  lineImage.color = invalidColor;  break;
            case ConnectionState.Complete: lineImage.color = completeColor; break;
            case ConnectionState.Dragging: lineImage.color = draggingColor; break;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
        if (eventData.button == PointerEventData.InputButton.Right)
            _map?.RemoveConnection(this);
    }

    public enum ConnectionState { Valid, Invalid, Complete, Dragging }
}
