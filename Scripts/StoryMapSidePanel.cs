using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// Правая боковая панель в сцене ментальной карты. Показывает:
///  - Description одной ноды (при клике на ноду).
///  - Черновик сюжета (описания всех нод, которые уже входят в связанную цепочку
///    от cat 0 в порядке соединения), когда игрок создал/удалил связь.
///
/// Поддерживает вертикальный скролл (через ScrollRect).
///
/// Настройка префаба:
/// 1. RectTransform справа, с дочерним ScrollRect.
/// 2. Внутри ScrollRect → Viewport → Content (с ContentSizeFitter по вертикали
///    и Vertical Layout Group) → TextMeshProUGUI (bodyText).
/// </summary>
public class StoryMapSidePanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private ScrollRect scrollRect;

    private void Awake()
    {
        if (root == null) root = gameObject;
        Hide();
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public void ShowSingleNode(StoryNode node)
    {
        if (node == null) { Hide(); return; }
        ShowText(node.description);
    }

    /// <summary>
    /// Показывает описание нод цепочки, в порядке cat 0 → cat N, разделяя абзацами.
    /// </summary>
    public void ShowChainDraft(System.Collections.Generic.List<StoryNode> chainNodes)
    {
        if (chainNodes == null || chainNodes.Count == 0) { Hide(); return; }

        var sb = new StringBuilder();
        for (int i = 0; i < chainNodes.Count; i++)
        {
            StoryNode n = chainNodes[i];
            if (n == null) continue;
            if (!string.IsNullOrEmpty(n.description))
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(n.description);
            }
        }

        if (sb.Length == 0) { Hide(); return; }
        ShowText(sb.ToString());
    }

    private void ShowText(string text)
    {
        if (root != null) root.SetActive(true);
        if (bodyText != null) bodyText.text = text;

        // Сбросить скролл в начало текста
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
