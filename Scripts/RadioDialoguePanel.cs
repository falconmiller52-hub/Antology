using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Панель диалога над радиоприёмником.
/// Показывает реплики по одной, переключается кликом по панели.
///
/// Настройка:
/// 1. Дочерний Canvas (World Space) от Radio, расположенный над спрайтом.
/// 2. Внутри — Panel с TextMeshProUGUI для текста реплики.
/// 3. Добавьте BoxCollider2D на Panel для обнаружения кликов.
/// 4. Повесьте этот скрипт на Panel.
/// </summary>
public class RadioDialoguePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject panelObject;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private CanvasGroup canvasGroup;

    private string[] _lines;
    private int _currentLineIndex;
    private System.Action _onFinished;

    private Camera _mainCamera;
    private bool _isVisible;
    private float _fadeTimer;

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (panelObject == null)
            panelObject = gameObject;

        if (canvasGroup == null)
            canvasGroup = GetComponentInParent<CanvasGroup>();
    }

    private void Update()
    {
        if (!_isVisible) return;

        // Плавное появление
        if (_fadeTimer < fadeInDuration)
        {
            _fadeTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(_fadeTimer / fadeInDuration);
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }

        // Клик для перехода к следующей реплике
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos);
            bool clickedPanel = false;
            foreach (var h in hits)
            {
                if (h.gameObject == gameObject) { clickedPanel = true; break; }
            }

            if (clickedPanel)
            {
                AdvanceLine();
            }
        }
    }

    /// <summary>
    /// Показывает диалог с набором реплик.
    /// </summary>
    public void Show(string[] lines, System.Action onFinished)
    {
        if (lines == null || lines.Length == 0) return;

        _lines = lines;
        _currentLineIndex = 0;
        _onFinished = onFinished;
        _isVisible = true;
        _fadeTimer = 0f;

        panelObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        DisplayCurrentLine();
    }

    /// <summary>
    /// Скрывает панель.
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
        panelObject.SetActive(false);
    }

    private void AdvanceLine()
    {
        _currentLineIndex++;

        if (_currentLineIndex >= _lines.Length)
        {
            // Диалог закончился
            _isVisible = false;
            _onFinished?.Invoke();
            return;
        }

        DisplayCurrentLine();
        AudioManager.Instance?.PlayButtonClick();
    }

    private void DisplayCurrentLine()
    {
        if (dialogueText != null && _lines != null && _currentLineIndex < _lines.Length)
        {
            dialogueText.text = _lines[_currentLineIndex];
        }
    }
}
