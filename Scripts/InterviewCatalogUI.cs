using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Каталог персоналий для интервью.
/// Генерирует кнопки из массива InterviewData.
///
/// Настройка:
/// 1. На CatalogPanel повесьте этот скрипт.
/// 2. npcContainer — Vertical Layout Group для кнопок.
/// 3. npcButtonPrefab — Button с Image (иконка) + TextMeshProUGUI (имя).
/// </summary>
public class InterviewCatalogUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform npcContainer;
    [SerializeField] private GameObject npcButtonPrefab;

    private InterviewManager _manager;

    public void Populate(InterviewData[] interviews, InterviewManager manager)
    {
        _manager = manager;

        foreach (Transform child in npcContainer)
            Destroy(child.gameObject);

        foreach (InterviewData data in interviews)
        {
            if (data.isCompleted) continue;

            GameObject btnObj = Instantiate(npcButtonPrefab, npcContainer);

            // Имя НПС
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = data.npcName;

            // Иконка НПС (если есть дочерний Image с тегом "Icon")
            Image[] images = btnObj.GetComponentsInChildren<Image>();
            foreach (Image img in images)
            {
                if (img.gameObject != btnObj && data.npcIcon != null)
                {
                    img.sprite = data.npcIcon;
                    break;
                }
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                InterviewData captured = data;
                btn.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButtonClick();
                    _manager.StartInterview(captured);
                });
            }
        }
    }
}
