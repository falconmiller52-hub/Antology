using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Кликабельная зона по краям панели для закрытия меню.
/// Вешается на UI Image (прозрачный или полупрозрачный), расположенный
/// сбоку от основной панели. При клике вызывает метод закрытия у указанной цели.
///
/// Режимы:
/// - InteractableItem: вызывает CloseMenu() у письма/газеты.
/// - InterviewCatalog: вызывает CloseCatalog() у InterviewManager (закрывает только каталог,
///   не диалог — диалог закрывается кнопкой [ЗАКОНЧИТЬ]).
/// - UnityEvent: гибкий режим, настраивается через инспектор (для ПК, паузы и т.д.).
///
/// Настройка:
/// 1. Внутри меню-панели создайте два Image по бокам (Raycast Target = ON).
/// 2. Повесьте этот скрипт на каждый.
/// 3. Выберите mode и назначьте ссылку (или UnityEvent).
/// 4. На Canvas должен быть Graphic Raycaster и EventSystem в сцене.
/// </summary>
public class MenuCloseZone : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum CloseMode
    {
        InteractableItem,
        InterviewCatalog,
        Custom
    }

    [Header("Mode")]
    [SerializeField] private CloseMode mode = CloseMode.InteractableItem;

    [Header("Target (InteractableItem mode)")]
    [Tooltip("Письмо или газета, чьё меню нужно закрыть")]
    [SerializeField] private InteractableItem targetItem;

    [Header("Target (InterviewCatalog mode)")]
    [Tooltip("InterviewManager, чей каталог нужно закрыть")]
    [SerializeField] private InterviewManager targetInterviewManager;

    [Header("Custom mode")]
    [Tooltip("Вызывается в режиме Custom — цепляйте сюда CloseAll/ResumeGame и т.п.")]
    [SerializeField] private UnityEvent onClose;

    [Header("Optional — cursor feedback")]
    [Tooltip("Менять курсор на hover при наведении (нужна ссылка на CustomCursor в сцене)")]
    [SerializeField] private bool updateCursor = true;

    private CustomCursor _customCursor;

    private void Awake()
    {
        if (updateCursor)
            _customCursor = FindFirstObjectByType<CustomCursor>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (mode)
        {
            case CloseMode.InteractableItem:
                if (targetItem != null)
                    targetItem.CloseMenu();
                else
                    Debug.LogWarning($"[MenuCloseZone] {name}: targetItem not assigned.");
                break;

            case CloseMode.InterviewCatalog:
                if (targetInterviewManager != null)
                    targetInterviewManager.CloseCatalog();
                else
                    Debug.LogWarning($"[MenuCloseZone] {name}: targetInterviewManager not assigned.");
                break;

            case CloseMode.Custom:
                onClose?.Invoke();
                break;
        }

        // После закрытия курсор вернётся к default при следующем наведении на мир,
        // но если указатель остался над этой зоной — она уже скрыта вместе с меню, так что ок.
        if (updateCursor && _customCursor != null)
            _customCursor.SetDefault();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (updateCursor && _customCursor != null)
            _customCursor.SetHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (updateCursor && _customCursor != null)
            _customCursor.SetDefault();
    }
}
