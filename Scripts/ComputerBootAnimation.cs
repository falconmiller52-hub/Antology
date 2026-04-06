using UnityEngine;
using System.Collections;

/// <summary>
/// Анимация включения компьютера.
/// Поверх спрайта ПК появляется дочерний спрайт экрана включения,
/// на котором проигрывается Animator-анимация или покадровая последовательность.
/// После завершения анимации экран показывает idle-спрайт и вызывается callback.
///
/// Настройка:
/// 1. На спрайте ПК — этот скрипт.
/// 2. Дочерний GameObject "BootScreen" со SpriteRenderer (спрайт экрана).
///    По умолчанию деактивирован или имеет offSprite.
/// 3. Если есть Animator на BootScreen — будет проигран триггер "Boot".
/// 4. Если нет Animator — используется массив bootFrames.
/// </summary>
public class ComputerBootAnimation : MonoBehaviour
{
    [Header("Screen Overlay")]
    [Tooltip("Дочерний SpriteRenderer экрана, который появляется поверх ПК")]
    [SerializeField] private SpriteRenderer screenRenderer;
    [SerializeField] private GameObject screenObject;

    [Header("Mode: Animator")]
    [SerializeField] private Animator screenAnimator;
    [SerializeField] private string bootTrigger = "Boot";

    [Header("Mode: Sprite Sequence (если нет Animator)")]
    [SerializeField] private Sprite[] bootFrames;
    [SerializeField] private float frameRate = 8f;

    [Header("Sprites")]
    [SerializeField] private Sprite offScreenSprite;
    [SerializeField] private Sprite idleScreenSprite;

    [Header("Settings")]
    [SerializeField] private float bootDuration = 2f;

    private bool _isBooting;

    /// <summary>
    /// Запускает анимацию включения. По завершении вызывается callback.
    /// </summary>
    public void PlayBoot(System.Action onComplete)
    {
        if (_isBooting) return;
        StartCoroutine(BootSequence(onComplete));
    }

    public void ShowOff()
    {
        if (screenObject != null)
            screenObject.SetActive(false);

        if (screenRenderer != null && offScreenSprite != null)
            screenRenderer.sprite = offScreenSprite;
    }

    public void ShowIdle()
    {
        if (screenObject != null)
            screenObject.SetActive(true);

        if (screenRenderer != null && idleScreenSprite != null)
            screenRenderer.sprite = idleScreenSprite;
    }

    private IEnumerator BootSequence(System.Action onComplete)
    {
        _isBooting = true;

        // Показываем экран поверх ПК
        if (screenObject != null)
            screenObject.SetActive(true);

        if (screenAnimator != null)
        {
            screenAnimator.SetTrigger(bootTrigger);
            yield return new WaitForSeconds(bootDuration);
        }
        else if (bootFrames != null && bootFrames.Length > 0 && screenRenderer != null)
        {
            float frameDuration = 1f / frameRate;
            float elapsed = 0f;
            int frameIndex = 0;

            while (elapsed < bootDuration)
            {
                screenRenderer.sprite = bootFrames[frameIndex % bootFrames.Length];
                frameIndex++;
                yield return new WaitForSeconds(frameDuration);
                elapsed += frameDuration;
            }
        }
        else
        {
            yield return new WaitForSeconds(bootDuration);
        }

        ShowIdle();
        _isBooting = false;
        onComplete?.Invoke();
    }
}
