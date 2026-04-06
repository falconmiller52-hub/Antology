using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Панель диалога над радиоприёмником с Undertale-стилем голоса.
/// Для каждого символа проигрывается voiceBlip с заданным pitch.
/// </summary>
public class RadioDialoguePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject panelObject;
    [SerializeField] private AudioSource blipSource;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.04f;
    [SerializeField] private int charsPerBlip = 2;

    [Header("Intel Highlight")]
    [SerializeField] private Color normalLineColor = Color.white;
    [SerializeField] private Color intelLineColor = new Color(1f, 0.6f, 0.6f, 1f);

    private string[] _lines;
    private int _currentLineIndex;
    private System.Action _onFinished;

    private AudioClip _voiceBlip;
    private float _voicePitch = 1f;
    private IntelKey[] _intelKeys;

    private Camera _mainCamera;
    private bool _isVisible;
    private bool _isTyping;
    private bool _skipTyping;
    private float _fadeTimer;
    private Coroutine _typeCoroutine;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (panelObject == null) panelObject = gameObject;
        if (canvasGroup == null) canvasGroup = GetComponentInParent<CanvasGroup>();

        // Авто-создаём AudioSource для блипов если не назначен
        if (blipSource == null)
        {
            blipSource = gameObject.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        if (!_isVisible) return;

        if (_fadeTimer < fadeInDuration)
        {
            _fadeTimer += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(_fadeTimer / fadeInDuration);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_isTyping)
            {
                _skipTyping = true;
            }
            else
            {
                AdvanceLine();
            }
        }
    }

    /// <summary>
    /// Показывает диалог с голосовым блипом и автоматическим маркированием.
    /// </summary>
    public void Show(string[] lines, System.Action onFinished, AudioClip voiceBlip = null, float voicePitch = 1f, IntelKey[] intelKeys = null)
    {
        if (lines == null || lines.Length == 0) return;

        _lines = lines;
        _currentLineIndex = 0;
        _onFinished = onFinished;
        _voiceBlip = voiceBlip;
        _voicePitch = voicePitch;
        _intelKeys = intelKeys;
        _isVisible = true;
        _fadeTimer = 0f;

        panelObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        DisplayCurrentLine();
    }

    public void Hide()
    {
        _isVisible = false;
        _isTyping = false;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        panelObject.SetActive(false);
    }

    private void AdvanceLine()
    {
        _currentLineIndex++;

        if (_currentLineIndex >= _lines.Length)
        {
            _isVisible = false;
            _onFinished?.Invoke();
            return;
        }

        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        // Determine if this line carries intel
        bool hasIntel = _intelKeys != null && _currentLineIndex < _intelKeys.Length && _intelKeys[_currentLineIndex] != null;

        // Set text color based on intel
        dialogueText.color = hasIntel ? intelLineColor : normalLineColor;

        // Auto-collect intel key for this line
        if (hasIntel)
        {
            IntelManager.Instance?.CollectKey(_intelKeys[_currentLineIndex]);
        }

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(_lines[_currentLineIndex]));
    }

    private IEnumerator TypeText(string line)
    {
        dialogueText.text = "";
        _isTyping = true;
        _skipTyping = false;

        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping)
            {
                dialogueText.text = line;
                break;
            }

            dialogueText.text = line.Substring(0, i + 1);

            // Voice blip каждые N символов (пропускаем пробелы)
            if (_voiceBlip != null && blipSource != null && !char.IsWhiteSpace(line[i]) && i % charsPerBlip == 0)
            {
                blipSource.pitch = _voicePitch + Random.Range(-0.05f, 0.05f);
                blipSource.PlayOneShot(_voiceBlip);
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;
    }
}
