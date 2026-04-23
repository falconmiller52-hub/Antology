using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-представление одной ноды на ментальной карте.
/// Все позиции (драг, позиции сокетов) считаются относительно mapArea —
/// единого RectTransform, в котором лежат и ноды, и нити.
/// </summary>
public class StoryNodeUI : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Visual")]
    [SerializeField] private Image bodyImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    [Header("Sockets")]
    [SerializeField] private StorySocketUI inputSocket;
    [SerializeField] private StorySocketUI outputSocket;

    [Header("Locked Visual")]
    [SerializeField] private Color lockedTint = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color unlockedTint = Color.white;

    public StoryNode Node { get; private set; }
    public bool IsUnlocked { get; private set; }
    public StorySocketUI InputSocket => inputSocket;
    public StorySocketUI OutputSocket => outputSocket;

    private RectTransform _rect;
    // Родительский контейнер карты (mapArea). В нём ноды и нити живут вместе,
    // благодаря чему все локальные координаты согласованы.
    private RectTransform _mapArea;
    private StoryMapUI _map;

    private Vector2 _dragOffset;

    public System.Action<StoryNodeUI> OnPositionChanged;
    public System.Action<StoryNodeUI> OnClicked;

    public void Initialize(StoryNode node, bool unlocked, RectTransform mapArea, StoryMapUI map)
    {
        Node = node;
        IsUnlocked = unlocked;
        _mapArea = mapArea;
        _map = map;
        _rect = GetComponent<RectTransform>();

        if (_rect.localScale == Vector3.zero)
            _rect.localScale = Vector3.one;
        if (_rect.sizeDelta.x <= 0f || _rect.sizeDelta.y <= 0f)
            _rect.sizeDelta = new Vector2(200f, 80f);

        if (labelText != null)
            labelText.text = unlocked ? node.label : "???";

        if (bodyImage != null)
        {
            bodyImage.sprite = unlocked ? unlockedSprite : lockedSprite;
            bodyImage.color = unlocked ? unlockedTint : lockedTint;
        }

        if (inputSocket != null)
            inputSocket.gameObject.SetActive(node.category > 0);
        if (outputSocket != null)
            outputSocket.gameObject.SetActive(node.category < 3);

        if (inputSocket != null && inputSocket.gameObject.activeSelf)
            inputSocket.Initialize(this, SocketType.Input, map);
        if (outputSocket != null && outputSocket.gameObject.activeSelf)
            outputSocket.Initialize(this, SocketType.Output, map);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsUnlocked) return;

        Vector2 pointerLocal;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapArea, eventData.position, eventData.pressEventCamera, out pointerLocal))
        {
            _dragOffset = (Vector2)_rect.localPosition - pointerLocal;
        }

        // Любой тап по ноде — показываем её description в sidePanel.
        // Даже если игрок потом начнёт драг — панель остаётся, скроется только
        // по клику в пустое место или при смене отображения на черновик.
        OnClicked?.Invoke(this);
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsUnlocked) return;

        Vector2 pointerLocal;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapArea, eventData.position, eventData.pressEventCamera, out pointerLocal))
        {
            _rect.localPosition = pointerLocal + _dragOffset;
            OnPositionChanged?.Invoke(this);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsUnlocked) return;
        OnPositionChanged?.Invoke(this);
    }

    /// <summary>
    /// Возвращает позицию сокета в локальных координатах mapArea.
    /// </summary>
    public Vector2 GetSocketLocalPosition(SocketType type)
    {
        RectTransform target = (type == SocketType.Input)
            ? (inputSocket != null ? inputSocket.GetComponent<RectTransform>() : null)
            : (outputSocket != null ? outputSocket.GetComponent<RectTransform>() : null);

        if (target == null) return _rect.localPosition;

        // Получаем мировую позицию сокета и конвертируем в координаты mapArea.
        // Делаем через WorldToScreen → ScreenToLocalPointInRectangle(mapArea), чтобы
        // учесть все вложенные трансформы корректно.
        Vector3 world = target.position;
        Canvas canvas = _mapArea.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                     ? canvas.worldCamera : null;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _mapArea, screen, cam, out local);
        return local;
    }
}

public enum SocketType
{
    Input,
    Output
}
