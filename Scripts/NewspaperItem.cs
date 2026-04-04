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
        // Газета помечается как прочитанная, но спрайт не меняется
        isOpened = true;
    }
}
