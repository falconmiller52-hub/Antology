using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Сцена титров (Title).
/// Текст титров растемняется и затемняется, сменяя друг друга.
/// После последнего титра — переход к MainMenu.
///
/// Настройка:
/// 1. Canvas с TextMeshProUGUI для текста титров (по центру).
/// 2. Повесьте этот скрипт.
/// </summary>
public class TitleScene : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Credits")]
    [TextArea(2, 5)]
    [SerializeField] private string[] creditLines;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float pauseBetween = 0.5f;

    [Header("Next Scene")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Start()
    {
        StartCoroutine(PlayCredits());
    }

    private IEnumerator PlayCredits()
    {
        // Начинаем прозрачным
        SetTextAlpha(0f);

        for (int i = 0; i < creditLines.Length; i++)
        {
            creditsText.text = creditLines[i];

            // Fade in
            yield return FadeText(0f, 1f, fadeInDuration);

            // Hold
            yield return new WaitForSeconds(holdDuration);

            // Fade out
            yield return FadeText(1f, 0f, fadeOutDuration);

            // Пауза между титрами
            if (i < creditLines.Length - 1)
                yield return new WaitForSeconds(pauseBetween);
        }

        // Переход в главное меню
        yield return new WaitForSeconds(1f);

        // Сбрасываем прогресс для новой игры
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ResetAll();

        SceneManager.LoadScene(mainMenuScene);
    }

    private IEnumerator FadeText(float from, float to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(from, to, t);
            SetTextAlpha(alpha);
            yield return null;
        }

        SetTextAlpha(to);
    }

    private void SetTextAlpha(float alpha)
    {
        Color c = creditsText.color;
        c.a = alpha;
        creditsText.color = c;
    }
}
