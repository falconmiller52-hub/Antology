using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Точка соединения на ячейке (Input слева / Output справа).
///
/// UX создания связи:
///  - Клик ЛКМ на сокете → начинается "вытягивание" (rubber band) линии.
///  - Пока тянем: линия обновляется каждый кадр до курсора.
///  - Клик ЛКМ на другом сокете → связь фиксируется, если совместима (cat N → cat N+1).
///  - ПКМ или Esc → отмена.
///
/// Сокет должен перехватывать клик (IPointerDownHandler), чтобы нода не начала
/// драгаться вместо создания связи. Используем Use=true в PointerDown — это
/// помечает событие как обработанное и нода не получит OnPointerDown.
/// </summary>
public class StorySocketUI : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual")]
    [SerializeField] private Image socketImage;
    [SerializeField] private Color idleColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.5f, 1f);

    public StoryNodeUI Owner { get; private set; }
    public SocketType Type { get; private set; }

    private StoryMapUI _map;
    private bool _isHovered;

    private void Awake()
    {
        if (socketImage == null)
            socketImage = GetComponent<Image>();
    }

    public void Initialize(StoryNodeUI owner, SocketType type, StoryMapUI map)
    {
        Owner = owner;
        Type = type;
        _map = map;
        SetIdle();
    }

    public bool IsHovered => _isHovered;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Owner == null || !Owner.IsUnlocked) return;
        if (_map == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Сначала проверим, тянется ли уже нить — если да, это попытка её замкнуть.
            if (_map.IsDraggingConnection)
            {
                _map.TryCompleteConnectionOn(this);
            }
            else
            {
                _map.BeginDraggingConnectionFrom(this);
            }

            // Помечаем событие обработанным — иначе OnPointerDown ноды тоже сработает
            // и нода начнёт драгаться вслед за курсором.
            eventData.Use();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Owner == null || !Owner.IsUnlocked) return;
        _isHovered = true;
        if (socketImage != null)
            socketImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        if (socketImage != null)
            socketImage.color = idleColor;
    }

    public void SetIdle()
    {
        _isHovered = false;
        if (socketImage != null)
            socketImage.color = idleColor;
    }

    public Vector2 GetLocalPosition() => Owner.GetSocketLocalPosition(Type);
}
