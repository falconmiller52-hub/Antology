using UnityEngine;

/// <summary>
/// Газета: после открытия меню спрайт НЕ меняется.
/// Всё остальное поведение (наведение, обводка, подъём, перетаскивание)
/// наследуется от InteractableItem.
/// 
/// Настройка в инспекторе:
/// - Normal Sprite: газета
/// - Outline Sprite: газета с обводкой
/// - Menu Panel: уникальная менюшка этой газеты
/// </summary>
public class NewspaperItem : InteractableItem
{
    protected override void OnMenuOpened()
    {
        isOpened = true;
        AudioManager.Instance?.PlayNewspaperOpen();
    }

    protected override void OnMenuClosed()
    {
        AudioManager.Instance?.PlayNewspaperClose();
    }
}
