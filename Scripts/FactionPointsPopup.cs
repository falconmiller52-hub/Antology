using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Всплывающая надпись очков фракций типа "+1A +2B −3C".
/// Появляется, подпрыгивает, плавно исчезает и самоуничтожается.
///
/// Настройка префаба:
/// 1. RectTransform с TextMeshProUGUI.
/// 2. Canvas Group (для плавного fade).
/// 3. Повесить этот скрипт.
/// </summary>
public class FactionPointsPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float lifeTime = 1.8f;
    [SerializeField] private float jumpHeight = 30f;
    [SerializeField] private float horizontalDrift = 10f;
    [SerializeField] private AnimationCurve positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.1f);

    [Header("Colors")]
    [SerializeField] private Color positiveColor = new Color(0.3f, 1f, 0.5f, 1f);
    [SerializeField] private Color negativeColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color neutralColor = new Color(1f, 1f, 0.7f, 1f);

    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Настраивает текст и стартует анимацию. По окончании объект уничтожается.
    /// </summary>
    public void Play(FactionDelta delta)
    {
        string text = BuildText(delta);
        if (string.IsNullOrEmpty(text))
        {
            Destroy(gameObject);
            return;
        }

        label.text = text;
        label.color = DetermineColor(delta);

        StartCoroutine(AnimateAndDestroy());
    }

    private static string BuildText(FactionDelta d)
    {
        var sb = new System.Text.StringBuilder();
        AppendPart(sb, d.a, "A");
        AppendPart(sb, d.b, "B");
        AppendPart(sb, d.c, "C");
        AppendPart(sb, d.d, "D");
        return sb.ToString().Trim();
    }

    private static void AppendPart(System.Text.StringBuilder sb, int value, string letter)
    {
        if (value == 0) return;
        if (sb.Length > 0) sb.Append(' ');
        sb.Append(value > 0 ? "+" : "");
        sb.Append(value);
        sb.Append(letter);
    }

    private Color DetermineColor(FactionDelta d)
    {
        int total = d.a + d.b + d.c + d.d;
        if (total > 0) return positiveColor;
        if (total < 0) return negativeColor;
        return neutralColor;
    }

    private IEnumerator AnimateAndDestroy()
    {
        Vector2 startPos = _rt.anchoredPosition;
        float driftSign = Random.value > 0.5f ? 1f : -1f;
        float timer = 0f;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / lifeTime);

            float y = jumpHeight * positionCurve.Evaluate(t);
            float x = horizontalDrift * driftSign * t;
            _rt.anchoredPosition = startPos + new Vector2(x, y);

            canvasGroup.alpha = fadeCurve.Evaluate(t);
            _rt.localScale = Vector3.one * scaleCurve.Evaluate(t);

            yield return null;
        }

        Destroy(gameObject);
    }
}
