using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Затемнение экрана (fade to black) с вызовом callback по завершении.
/// Используется перед сменой сцен.
///
/// Настройка:
/// 1. В каждой сцене, где нужно затемнение: Canvas (Sort Order = 100).
/// 2. Image на весь экран, чёрный, alpha = 0, Raycast Target = ON (блокирует клики).
/// 3. Повесьте этот скрипт на Image.
/// 4. По умолчанию Image неактивен — активируется при вызове FadeAndExecute.
/// </summary>
public class SceneFadeOut : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Image _fadeImage;
    private float _timer;
    private bool _isFading;
    private System.Action _onComplete;

    private void Awake()
    {
        _fadeImage = GetComponent<Image>();
        SetAlpha(0f);
        _fadeImage.raycastTarget = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isFading) return;

        _timer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_timer / fadeDuration);
        float alpha = fadeCurve.Evaluate(t);
        SetAlpha(alpha);

        if (t >= 1f)
        {
            _isFading = false;
            _onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Запускает затемнение. По завершении вызывается callback.
    /// </summary>
    public void FadeAndExecute(System.Action onComplete)
    {
        gameObject.SetActive(true);
        _fadeImage.raycastTarget = true;
        _onComplete = onComplete;
        _timer = 0f;
        _isFading = true;
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        Color c = _fadeImage.color;
        c.a = alpha;
        _fadeImage.color = c;
    }
}
