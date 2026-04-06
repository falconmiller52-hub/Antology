using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер сцены Intermedia (вещание).
/// Показывает картинку радиостанции и текст вещания реплика за репликой.
/// Порядок: приветствие → сюжеты игрока → прощание → затемнение → следующий день.
///
/// Настройка:
/// 1. Сцена Intermedia: Canvas с Image (радиостанция) и TextMeshProUGUI (текст).
/// 2. Повесьте этот скрипт на Canvas или отдельный GameObject.
/// 3. Назначьте тексты приветствия и прощания.
/// 4. Добавьте SceneFadeIn (растемнение) и SceneFadeOut (затемнение) в сцену.
/// </summary>
public class BroadcastScene : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI broadcastText;
    [SerializeField] private Image radioImage;

    [Header("Broadcast Texts")]
    [TextArea(2, 4)]
    [SerializeField] private string[] greetingLines;
    [TextArea(2, 4)]
    [SerializeField] private string[] farewellLines;

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.04f;

    private List<string> _allLines = new List<string>();
    private int _currentLineIndex;
    private bool _isTyping;
    private bool _skipTyping;
    private bool _isDone;
    private Coroutine _typeCoroutine;

    private void Start()
    {
        // Сирена в начале вещания
        AudioManager.Instance?.PlaySiren();

        BuildLinesList();
        _currentLineIndex = 0;
        ShowLine(_currentLineIndex);
    }

    private void Update()
    {
        if (_isDone) return;

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

    private void BuildLinesList()
    {
        _allLines.Clear();

        // Приветствие
        if (greetingLines != null)
        {
            foreach (string line in greetingLines)
                _allLines.Add(line);
        }

        // Сюжеты игрока
        if (GameProgressManager.Instance != null)
        {
            string[] stories = GameProgressManager.Instance.GetTodayStories();
            if (stories != null)
            {
                foreach (string story in stories)
                {
                    if (!string.IsNullOrEmpty(story))
                        _allLines.Add(story);
                }
            }
        }

        // Прощание
        if (farewellLines != null)
        {
            foreach (string line in farewellLines)
                _allLines.Add(line);
        }
    }

    private void ShowLine(int index)
    {
        if (index >= _allLines.Count)
        {
            OnBroadcastEnd();
            return;
        }

        _skipTyping = false;
        _isTyping = true;

        if (_typeCoroutine != null)
            StopCoroutine(_typeCoroutine);

        _typeCoroutine = StartCoroutine(TypeText(_allLines[index]));
    }

    private IEnumerator TypeText(string line)
    {
        broadcastText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping)
            {
                broadcastText.text = line;
                break;
            }

            broadcastText.text = line.Substring(0, i + 1);
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;
    }

    private void AdvanceLine()
    {
        _currentLineIndex++;
        AudioManager.Instance?.PlayButtonClick();
        ShowLine(_currentLineIndex);
    }

    private void OnBroadcastEnd()
    {
        _isDone = true;

        SceneFadeOut fader = FindFirstObjectByType<SceneFadeOut>();
        if (fader != null)
        {
            fader.FadeAndExecute(() => GameProgressManager.Instance?.OnBroadcastFinished());
        }
        else
        {
            GameProgressManager.Instance?.OnBroadcastFinished();
        }
    }
}
