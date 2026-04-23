using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер сцены Intermedia (вещание).
/// Порядок: приветствие → сюжеты игрока (каждая нода — отдельная реплика с popup
/// очков фракций) → прощание → итоговый счётчик → затемнение → следующий день.
///
/// Клик ЛКМ — промотка реплики, затем переход к следующей.
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

    [Header("Faction Points Popup")]
    [Tooltip("Префаб FactionPointsPopup — spawn'ится над broadcastText при показе каждой ноды.")]
    [SerializeField] private GameObject pointsPopupPrefab;
    [Tooltip("Контейнер, где будут появляться popup'ы. Если не задан — spawn в родителе broadcastText.")]
    [SerializeField] private RectTransform popupContainer;
    [Tooltip("Смещение точки spawn'а относительно позиции broadcastText.")]
    [SerializeField] private Vector2 popupOffset = new Vector2(0, 60);

    [Header("Final Score Display")]
    [Tooltip("TMP, который показывается ПОСЛЕ прощания. Можно оставить пустым.")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private string finalScoreFormat = "{A}A  {B}B  {C}C  {D}D";
    [SerializeField] private float finalScoreShowDuration = 2.5f;

    private List<string> _allLines = new List<string>();
    private List<FactionDelta?> _lineDeltas = new List<FactionDelta?>();

    private int _currentLineIndex;
    private bool _isTyping;
    private bool _skipTyping;
    private bool _isDone;
    private Coroutine _typeCoroutine;

    private void Start()
    {
        AudioManager.Instance?.PlaySiren();

        if (finalScoreText != null) finalScoreText.gameObject.SetActive(false);

        BuildLinesList();
        _currentLineIndex = 0;
        ShowLine(_currentLineIndex);
    }

    private void Update()
    {
        if (_isDone) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_isTyping) _skipTyping = true;
            else AdvanceLine();
        }
    }

    private void BuildLinesList()
    {
        _allLines.Clear();
        _lineDeltas.Clear();

        // Приветствие (без popup'ов)
        if (greetingLines != null)
        {
            foreach (string line in greetingLines)
            {
                _allLines.Add(line);
                _lineDeltas.Add(null);
            }
        }

        int greetingCount = _allLines.Count;

        // Сюжеты игрока — каждая нода = реплика + popup
        int storyLinesAdded = 0;
        if (GameProgressManager.Instance != null)
        {
            List<string[]> storyBlocks = GameProgressManager.Instance.GetTodayStoryBlocks();
            List<FactionDelta[]> storyDeltas = GameProgressManager.Instance.GetTodayStoryDeltas();

            if (storyBlocks != null)
            {
                for (int s = 0; s < storyBlocks.Count; s++)
                {
                    string[] blocks = storyBlocks[s];
                    FactionDelta[] deltas = (storyDeltas != null && s < storyDeltas.Count)
                                             ? storyDeltas[s] : null;
                    if (blocks == null) continue;

                    for (int b = 0; b < blocks.Length; b++)
                    {
                        if (string.IsNullOrEmpty(blocks[b])) continue;
                        _allLines.Add(blocks[b]);

                        FactionDelta? delta = (deltas != null && b < deltas.Length)
                                               ? (FactionDelta?)deltas[b] : null;
                        _lineDeltas.Add(delta);
                        storyLinesAdded++;
                    }
                }
            }
        }
        else
        {
            Debug.LogError("[Broadcast] GameProgressManager.Instance is NULL.");
        }

        // Прощание (без popup'ов)
        if (farewellLines != null)
        {
            foreach (string line in farewellLines)
            {
                _allLines.Add(line);
                _lineDeltas.Add(null);
            }
        }

        Debug.Log($"[Broadcast] Lines prepared: greeting={greetingCount}, " +
                  $"stories={storyLinesAdded}, farewell={(farewellLines != null ? farewellLines.Length : 0)}, " +
                  $"total={_allLines.Count}");
    }

    private void ShowLine(int index)
    {
        if (index >= _allLines.Count)
        {
            OnAllLinesDone();
            return;
        }

        _skipTyping = false;
        _isTyping = true;

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(_allLines[index]));

        if (_lineDeltas[index].HasValue)
            SpawnPointsPopup(_lineDeltas[index].Value);
    }

    private IEnumerator TypeText(string line)
    {
        broadcastText.text = "";
        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping) { broadcastText.text = line; break; }
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

    private void SpawnPointsPopup(FactionDelta delta)
    {
        if (pointsPopupPrefab == null) return;
        if (delta.a == 0 && delta.b == 0 && delta.c == 0 && delta.d == 0) return;

        RectTransform parent = popupContainer;
        if (parent == null && broadcastText != null)
            parent = broadcastText.transform.parent as RectTransform;
        if (parent == null) return;

        GameObject obj = Instantiate(pointsPopupPrefab, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null && broadcastText != null)
            rt.anchoredPosition = (Vector2)broadcastText.rectTransform.anchoredPosition + popupOffset;

        FactionPointsPopup popup = obj.GetComponent<FactionPointsPopup>();
        if (popup != null) popup.Play(delta);
    }

    private void OnAllLinesDone()
    {
        if (finalScoreText != null)
            StartCoroutine(ShowFinalScoreThenFinish());
        else
            Finish();
    }

    private IEnumerator ShowFinalScoreThenFinish()
    {
        finalScoreText.gameObject.SetActive(true);
        int a = 0, b = 0, c = 0, d = 0;
        if (GameProgressManager.Instance != null)
        {
            a = GameProgressManager.Instance.FactionAScore;
            b = GameProgressManager.Instance.FactionBScore;
            c = GameProgressManager.Instance.FactionCScore;
            d = GameProgressManager.Instance.FactionDScore;
        }
        finalScoreText.text = finalScoreFormat
            .Replace("{A}", a.ToString())
            .Replace("{B}", b.ToString())
            .Replace("{C}", c.ToString())
            .Replace("{D}", d.ToString());

        yield return new WaitForSeconds(finalScoreShowDuration);
        Finish();
    }

    private void Finish()
    {
        _isDone = true;
        Debug.Log("[Broadcast] Broadcast ended.");

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
