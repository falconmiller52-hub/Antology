using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-представление одной ноды на ментальной карте.
/// Поддерживает:
/// - Перетаскивание по карте (драг основной части).
/// - Два "сокета": левый (вход) и правый (выход) для создания связей.
/// - Визуальное состояние: заблокирована (нет ключа) / доступна / выбрана.
///
/// Настройка префаба:
/// 1. Root: RectTransform + CanvasGroup.
/// 2. Дочерний Image "Body" — спрайт ячейки.
/// 3. Дочерний TextMeshProUGUI "Label".
/// 4. Два дочерних Image "InputSocket" (слева) и "OutputSocket" (справа)
///    с компонентом StorySocketUI.
/// </summary>
public class StoryNodeUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Visual")]
    [SerializeField] private Image bodyImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    [Header("Sockets")]
    [SerializeField] private StorySocketUI inputSocket;   // слева (cat N принимает от cat N-1)
    [SerializeField] private StorySocketUI outputSocket;  // справа (cat N соединяется с cat N+1)

    [Header("Locked Visual")]
    [SerializeField] private Color lockedTint = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color unlockedTint = Color.white;

    public StoryNode Node { get; private set; }
    public bool IsUnlocked { get; private set; }
    public StorySocketUI InputSocket => inputSocket;
    public StorySocketUI OutputSocket => outputSocket;

    private RectTransform _rect;
    private RectTransform _canvasRect;
    private StoryMapUI _map;

    public System.Action<StoryNodeUI> OnPositionChanged;

    public void Initialize(StoryNode node, bool unlocked, RectTransform canvasRect, StoryMapUI map)
    {
        Node = node;
        IsUnlocked = unlocked;
        _canvasRect = canvasRect;
        _map = map;
        _rect = GetComponent<RectTransform>();

        if (labelText != null)
            labelText.text = unlocked ? node.label : "???";

        if (bodyImage != null)
        {
            bodyImage.sprite = unlocked ? unlockedSprite : lockedSprite;
            bodyImage.color = unlocked ? unlockedTint : lockedTint;
        }

        // Категория 0 (корневая) не имеет входного сокета — её никто не "соединяет с ней слева".
        if (inputSocket != null)
            inputSocket.gameObject.SetActive(node.category > 0);

        // Категория 3 (последняя) не имеет выходного сокета — цепочка здесь заканчивается.
        if (outputSocket != null)
            outputSocket.gameObject.SetActive(node.category < 3);

        if (inputSocket != null)
            inputSocket.Initialize(this, SocketType.Input, map);
        if (outputSocket != null)
            outputSocket.Initialize(this, SocketType.Output, map);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsUnlocked) return;

        // Если клик начался на сокете — не драгаем саму ячейку.
        // (IPointerDownHandler на сокете перехватывает ранее.)
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsUnlocked) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            _rect.localPosition = localPoint;
            OnPositionChanged?.Invoke(this);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsUnlocked) return;
        OnPositionChanged?.Invoke(this);
    }

    /// <summary>
    /// Возвращает мировую (canvas local) позицию указанного сокета.
    /// Используется для рисования нитей.
    /// </summary>
    public Vector2 GetSocketLocalPosition(SocketType type)
    {
        RectTransform target = (type == SocketType.Input)
            ? inputSocket.GetComponent<RectTransform>()
            : outputSocket.GetComponent<RectTransform>();

        if (target == null) return _rect.localPosition;

        // Получаем мировую позицию сокета и конвертируем в local координаты canvas.
        Vector3 world = target.position;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            RectTransformUtility.WorldToScreenPoint(null, world),
            null,
            out local);
        return local;
    }
}

public enum SocketType
{
    Input,
    Output
}
