using UnityEngine;

/// <summary>
/// Письмо: после первого открытия меню спрайт меняется на "раскрытое".
/// У раскрытого письма своя обводка, оно также поднимается при наведении
/// и может быть открыто повторно.
/// 
/// Настройка в инспекторе:
/// - Normal Sprite: закрытое письмо
/// - Outline Sprite: закрытое письмо с обводкой
/// - Opened Sprite: раскрытое письмо
/// - Opened Outline Sprite: раскрытое письмо с обводкой
/// </summary>
public class LetterItem : InteractableItem
{
    [Header("Opened State Sprites")]
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private Sprite openedOutlineSprite;

    protected override Sprite GetHoverSprite()
    {
        if (isOpened && openedOutlineSprite != null)
            return openedOutlineSprite;

        return outlineSprite;
    }

    protected override Sprite GetNormalSprite()
    {
        if (isOpened && openedSprite != null)
            return openedSprite;

        return normalSprite;
    }

    protected override void OnMenuOpened()
    {
        base.OnMenuOpened();

        // Сразу обновляем спрайт на "раскрытый"
        if (openedSprite != null)
            spriteRenderer.sprite = openedOutlineSprite != null ? openedOutlineSprite : openedSprite;
    }
}
