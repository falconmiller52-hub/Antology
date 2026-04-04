using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Растемнение (fade from black) при загрузке сцены.
/// 
/// Настройка:
/// 1. В сцене Gameplay создайте Canvas (Screen Space - Overlay, Sort Order = 100).
/// 2. Добавьте дочерний Image, растяните на весь экран (stretch-stretch).
/// 3. Цвет Image = чёрный (0,0,0,1). Raycast Target = OFF.
/// 4. Повесьте этот скрипт на этот Image.
/// </summary>
public class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Image _fadeImage;
    private float _timer;
    private bool _isDone;

    private void Awake()
    {
        _fadeImage = GetComponent<Image>();

        // Начинаем полностью чёрным
        SetAlpha(1f);
        _fadeImage.raycastTarget = false;
    }

    private void Update()
    {
        if (_isDone) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / fadeDuration);
        float alpha = 1f - fadeCurve.Evaluate(t);

        SetAlpha(alpha);

        if (t >= 1f)
        {
            _isDone = true;
            gameObject.SetActive(false);
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = _fadeImage.color;
        c.a = alpha;
        _fadeImage.color = c;
    }
}
