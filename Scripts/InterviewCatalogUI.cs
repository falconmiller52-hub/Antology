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

        Debug.Log($"[InterviewCatalog] Populating with {interviews.Length} interviews");

        foreach (InterviewData data in interviews)
        {
            Debug.Log($"[InterviewCatalog] NPC '{data.npcName}' isCompleted={data.isCompleted}");

            if (data.isCompleted) continue;

            GameObject btnObj = Instantiate(npcButtonPrefab, npcContainer);

            // Имя НПС
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = data.npcName;

            // Описание НПС (если есть второй TextMeshProUGUI)
            TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 1 && !string.IsNullOrEmpty(data.npcDescription))
                texts[1].text = data.npcDescription;

            // Иконка НПС — ищем дочерний Image с именем "Icon" или "NpcIcon"
            Image[] images = btnObj.GetComponentsInChildren<Image>();
            foreach (Image img in images)
            {
                if (img.gameObject != btnObj && data.npcIcon != null
                    && (img.gameObject.name == "Icon" || img.gameObject.name == "NpcIcon"))
                {
                    img.sprite = data.npcIcon;
                    img.preserveAspect = true;
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
