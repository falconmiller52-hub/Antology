using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Кнопка "Завершить смену" в меню компьютера.
/// Неактивна, пока не написано нужное количество сюжетов.
/// После нажатия — затемнение и переход к Intermedia.
///
/// Настройка:
/// 1. Button внутри TopicListPanel (или отдельная панель).
/// 2. Назначьте activeSprite/inactiveSprite или используйте Button interactable.
/// 3. Назначьте fadeOut (SceneFadeOut) для плавного затемнения.
/// </summary>
public class EndShiftButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Sprites")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    [Header("Colors")]
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void Start()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    /// <summary>
    /// Вызывается для обновления состояния кнопки.
    /// Проверяйте после каждого отправленного сюжета.
    /// </summary>
    public void UpdateState()
    {
        bool canEnd = GameProgressManager.Instance != null && GameProgressManager.Instance.CanEndShift;

        button.interactable = canEnd;

        if (buttonImage != null)
            buttonImage.sprite = canEnd ? activeSprite : inactiveSprite;

        if (buttonText != null)
            buttonText.color = canEnd ? activeTextColor : inactiveTextColor;
    }

    private void OnClicked()
    {
        if (GameProgressManager.Instance == null || !GameProgressManager.Instance.CanEndShift)
            return;

        AudioManager.Instance?.PlayPCButton();

        // Затемнение и переход
        SceneFadeOut fader = FindFirstObjectByType<SceneFadeOut>();
        if (fader != null)
        {
            fader.FadeAndExecute(() => GameProgressManager.Instance.EndShift());
        }
        else
        {
            GameProgressManager.Instance.EndShift();
        }
    }
}
