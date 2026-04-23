using UnityEngine;
using TMPro;

/// <summary>
/// Счётчик очков фракций в меню ПК.
/// Показывает очки фракций, зафиксированные на начало текущего дня
/// (FactionAScoreAtDayStart из GameProgressManager).
///
/// Не обновляется в процессе дня — только при загрузке новой Gameplay-сцены
/// (после Intermedia и событий).
///
/// Настройка:
/// 1. TMP-текст в правом верхнем углу StoryMapPanel/TopicListPanel.
/// 2. Повесить этот скрипт, назначить scoreText.
/// 3. Объект можно размножить — он сам обновляется в OnEnable.
/// </summary>
public class ComputerScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Формат строки. Плейсхолдеры: {A}, {B}, {C}, {D}.")]
    [SerializeField] private string format = "{A}A {B}B {C}C {D}D";

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (scoreText == null) return;

        int a, b, c, d;
        if (GameProgressManager.Instance != null)
        {
            a = GameProgressManager.Instance.FactionAScoreAtDayStart;
            b = GameProgressManager.Instance.FactionBScoreAtDayStart;
            c = GameProgressManager.Instance.FactionCScoreAtDayStart;
            d = GameProgressManager.Instance.FactionDScoreAtDayStart;
        }
        else
        {
            a = b = c = d = 0;
        }

        scoreText.text = format
            .Replace("{A}", a.ToString())
            .Replace("{B}", b.ToString())
            .Replace("{C}", c.ToString())
            .Replace("{D}", d.ToString());
    }
}
