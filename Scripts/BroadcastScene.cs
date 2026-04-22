using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер сцены Intermedia (вещание).
/// Порядок: приветствие → сюжеты игрока (по порядку, каждая ячейка — отдельная реплика)
/// → прощание → затемнение → следующий день.
///
/// Клик ЛКМ — промотка текущей реплики. Если реплика уже написана, клик переходит
/// к следующей.
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
                _skipTyping = true;
            else
                AdvanceLine();
        }
    }

    private void BuildLinesList()
    {
        _allLines.Clear();

        // Приветствие
        if (greetingLines != null)
            foreach (string line in greetingLines)
                _allLines.Add(line);

        int greetingCount = _allLines.Count;

        // Сюжеты игрока — каждая ячейка цепочки = отдельная реплика
        int storyLinesAdded = 0;
        if (GameProgressManager.Instance != null)
        {
            List<string[]> storyBlocks = GameProgressManager.Instance.GetTodayStoryBlocks();
            Debug.Log($"[Broadcast] BuildLinesList: storyBlocks count = " +
                      $"{(storyBlocks != null ? storyBlocks.Count : 0)}");

            if (storyBlocks != null)
            {
                for (int s = 0; s < storyBlocks.Count; s++)
                {
                    string[] blocks = storyBlocks[s];
                    if (blocks == null)
                    {
                        Debug.LogWarning($"[Broadcast] Story #{s} is null.");
                        continue;
                    }
                    Debug.Log($"[Broadcast] Story #{s}: {blocks.Length} texts.");

                    foreach (string block in blocks)
                    {
                        if (!string.IsNullOrEmpty(block))
                        {
                            _allLines.Add(block);
                            storyLinesAdded++;
                        }
                        else
                        {
                            Debug.LogWarning("[Broadcast] Skipped empty story text — check " +
                                             "StoryNode.description is filled.");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("[Broadcast] GameProgressManager.Instance is NULL. " +
                           "Убедитесь, что GameProgressManager создаётся в MainMenu и использует DontDestroyOnLoad.");
        }

        // Прощание
        if (farewellLines != null)
            foreach (string line in farewellLines)
                _allLines.Add(line);

        Debug.Log($"[Broadcast] Total lines prepared: greeting={greetingCount}, " +
                  $"stories={storyLinesAdded}, farewell={(farewellLines != null ? farewellLines.Length : 0)}, " +
                  $"total={_allLines.Count}");
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
        Debug.Log("[Broadcast] Broadcast ended. Attempting to finish...");

        SceneFadeOut fader = FindFirstObjectByType<SceneFadeOut>();
        if (fader != null)
        {
            fader.FadeAndExecute(() =>
            {
                if (GameProgressManager.Instance != null)
                    GameProgressManager.Instance.OnBroadcastFinished();
                else
                    QuitGame();
            });
        }
        else
        {
            if (GameProgressManager.Instance != null)
                GameProgressManager.Instance.OnBroadcastFinished();
            else
                QuitGame();
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
