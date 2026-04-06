using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Контроллер сцены концовки (EndingA или EndingB).
/// Показывает текст реплика за репликой (клик для промотки).
/// После последней реплики — затемнение и переход к титрам.
///
/// Настройка:
/// 1. Canvas с TextMeshProUGUI для текста концовки.
/// 2. Повесьте этот скрипт.
/// 3. Добавьте SceneFadeIn и SceneFadeOut.
/// </summary>
public class EndingScene : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI endingText;

    [Header("Ending Lines")]
    [TextArea(2, 5)]
    [SerializeField] private string[] lines;

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.04f;

    [Header("Next Scene")]
    [SerializeField] private string titleScene = "Title";

    private int _currentLineIndex;
    private bool _isTyping;
    private bool _skipTyping;
    private bool _isDone;
    private Coroutine _typeCoroutine;

    private void Start()
    {
        _currentLineIndex = 0;
        ShowLine(0);
    }

    private void Update()
    {
        if (_isDone) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_isTyping)
                _skipTyping = true;
            else
                AdvanceLine();
        }
    }

    private void ShowLine(int index)
    {
        if (index >= lines.Length)
        {
            OnEndingFinished();
            return;
        }

        _skipTyping = false;
        _isTyping = true;

        if (_typeCoroutine != null)
            StopCoroutine(_typeCoroutine);

        _typeCoroutine = StartCoroutine(TypeText(lines[index]));
    }

    private IEnumerator TypeText(string line)
    {
        endingText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping)
            {
                endingText.text = line;
                break;
            }

            endingText.text = line.Substring(0, i + 1);
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;
    }

    private void AdvanceLine()
    {
        _currentLineIndex++;
        ShowLine(_currentLineIndex);
    }

    private void OnEndingFinished()
    {
        _isDone = true;

        SceneFadeOut fader = FindFirstObjectByType<SceneFadeOut>();
        if (fader != null)
            fader.FadeAndExecute(() => SceneManager.LoadScene(titleScene));
        else
            SceneManager.LoadScene(titleScene);
    }
}
