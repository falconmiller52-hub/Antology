using UnityEngine;
using System.Collections;

/// <summary>
/// Анимация включения компьютера.
/// Проигрывает Animator-анимацию на спрайте ПК, затем вызывает callback.
///
/// Настройка:
/// 1. На спрайте ПК должен быть Animator с анимацией "Boot".
/// 2. Анимация "Boot" — последовательность спрайтов включения (2-3 секунды).
/// 3. В конце анимации — событие или просто ждём длительность.
/// Альтернатива: если нет Animator, используется массив спрайтов.
/// </summary>
public class ComputerBootAnimation : MonoBehaviour
{
    [Header("Mode: Animator")]
    [SerializeField] private Animator pcAnimator;
    [SerializeField] private string bootTrigger = "Boot";

    [Header("Mode: Sprite Sequence (если нет Animator)")]
    [SerializeField] private SpriteRenderer pcRenderer;
    [SerializeField] private Sprite[] bootFrames;
    [SerializeField] private float frameRate = 8f;

    [Header("Settings")]
    [SerializeField] private float bootDuration = 2f;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite offSprite;

    private bool _isBooting;

    /// <summary>
    /// Запускает анимацию включения. По завершении вызывается callback.
    /// </summary>
    public void PlayBoot(System.Action onComplete)
    {
        if (_isBooting) return;
        StartCoroutine(BootSequence(onComplete));
    }

    /// <summary>
    /// Показывает спрайт выключенного ПК.
    /// </summary>
    public void ShowOff()
    {
        if (pcRenderer != null && offSprite != null)
            pcRenderer.sprite = offSprite;
    }

    /// <summary>
    /// Показывает спрайт включённого ПК (после загрузки).
    /// </summary>
    public void ShowIdle()
    {
        if (pcRenderer != null && idleSprite != null)
            pcRenderer.sprite = idleSprite;
    }

    private IEnumerator BootSequence(System.Action onComplete)
    {
        _isBooting = true;

        if (pcAnimator != null)
        {
            // Режим Animator
            pcAnimator.SetTrigger(bootTrigger);
            yield return new WaitForSeconds(bootDuration);
        }
        else if (bootFrames != null && bootFrames.Length > 0 && pcRenderer != null)
        {
            // Режим последовательности спрайтов
            float frameDuration = 1f / frameRate;
            float elapsed = 0f;
            int frameIndex = 0;

            while (elapsed < bootDuration)
            {
                pcRenderer.sprite = bootFrames[frameIndex % bootFrames.Length];
                frameIndex++;
                yield return new WaitForSeconds(frameDuration);
                elapsed += frameDuration;
            }
        }
        else
        {
            // Нет анимации — просто ждём
            yield return new WaitForSeconds(bootDuration);
        }

        ShowIdle();
        _isBooting = false;
        onComplete?.Invoke();
    }
}
