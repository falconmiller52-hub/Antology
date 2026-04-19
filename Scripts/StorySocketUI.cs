using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Точка соединения на ячейке: левая (Input) или правая (Output).
/// Игрок кликает ЛКМ сначала на Output одной ячейки, затем на Input другой —
/// создаётся связь (если разрешена правилами).
///
/// Настройка:
/// 1. Дочерний Image внутри StoryNodeUI префаба.
/// 2. Raycast Target = ON.
/// 3. Повесьте этот скрипт.
/// </summary>
public class StorySocketUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual")]
    [SerializeField] private Image socketImage;
    [SerializeField] private Color idleColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.5f, 1f);
    [SerializeField] private Color armedColor = new Color(0.3f, 0.8f, 1f, 1f);

    public StoryNodeUI Owner { get; private set; }
    public SocketType Type { get; private set; }

    private StoryMapUI _map;
    private bool _isArmed;

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
        SetArmed(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Owner == null || !Owner.IsUnlocked) return;
        if (_map == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _map.OnSocketClicked(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Owner == null || !Owner.IsUnlocked) return;
        if (socketImage != null && !_isArmed)
            socketImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (socketImage != null && !_isArmed)
            socketImage.color = idleColor;
    }

    public void SetArmed(bool armed)
    {
        _isArmed = armed;
        if (socketImage != null)
            socketImage.color = armed ? armedColor : idleColor;
    }

    public Vector2 GetLocalPosition()
    {
        return Owner.GetSocketLocalPosition(Type);
    }
}
