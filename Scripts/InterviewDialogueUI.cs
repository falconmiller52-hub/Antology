using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Экран диалога интервью.
/// Сверху: иконка НПС + панель реплики НПС (печатается посимвольно).
/// Снизу: панель ответов игрока (кнопки сверху вниз).
///
/// Клик по области НПС-реплики — моментально допечатывает текст.
/// После допечатки появляются варианты ответов.
/// Циклические узлы: после ответа НПС повторяет исходную реплику узла.
///
/// Настройка:
/// 1. На DialoguePanel повесьте этот скрипт.
/// 2. Верхняя часть: npcPanel (Panel), npcIcon (Image), npcNameText, npcLineText.
/// 3. Нижняя часть: playerPanel (Panel), responsesContainer (Vertical Layout Group).
/// 4. responseButtonPrefab — Button с TextMeshProUGUI.
/// 5. npcPanel должен иметь BoxCollider2D или Button для обнаружения кликов.
/// </summary>
public class InterviewDialogueUI : MonoBehaviour
{
    [Header("NPC Panel (Top)")]
    [SerializeField] private GameObject npcPanel;
    [SerializeField] private Image npcIcon;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI npcLineText;
    [SerializeField] private Button npcPanelClickArea;

    [Header("Player Panel (Bottom)")]
    [SerializeField] private GameObject playerPanel;
    [SerializeField] private Transform responsesContainer;
    [SerializeField] private GameObject responseButtonPrefab;

    [Header("Typewriter Settings")]
    [SerializeField] private float typeSpeed = 0.03f;

    private InterviewData _data;
    private System.Action<InterviewData> _onFinished;

    private int _currentNodeIndex;
    private bool _isTyping;
    private bool _skipTyping;
    private string _fullLine;
    private Coroutine _typeCoroutine;

    public void Initialize(InterviewData data, System.Action<InterviewData> onFinished)
    {
        _data = data;
        _onFinished = onFinished;
        _currentNodeIndex = 0;

        // Настраиваем НПС
        if (npcIcon != null && data.npcIcon != null)
            npcIcon.sprite = data.npcIcon;
        if (npcNameText != null)
            npcNameText.text = data.npcName;

        // Клик по панели НПС для пропуска печати
        npcPanelClickArea.onClick.RemoveAllListeners();
        npcPanelClickArea.onClick.AddListener(OnNpcPanelClicked);

        // Скрываем ответы
        HidePlayerResponses();

        // Начинаем диалог
        ShowNode(_currentNodeIndex);
    }

    private void ShowNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= _data.nodes.Length)
        {
            EndInterview();
            return;
        }

        _currentNodeIndex = nodeIndex;
        DialogueNode node = _data.nodes[nodeIndex];

        HidePlayerResponses();
        StartTyping(node.npcLine);
    }

    private void StartTyping(string line)
    {
        _fullLine = line;
        _skipTyping = false;
        _isTyping = true;

        if (_typeCoroutine != null)
            StopCoroutine(_typeCoroutine);

        _typeCoroutine = StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string line)
    {
        npcLineText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping)
            {
                npcLineText.text = line;
                break;
            }

            npcLineText.text = line.Substring(0, i + 1);
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;
        OnTypingComplete();
    }

    private void OnNpcPanelClicked()
    {
        if (_isTyping)
        {
            // Допечатать моментально
            _skipTyping = true;
        }
    }

    private void OnTypingComplete()
    {
        // Показываем варианты ответов
        DialogueNode node = _data.nodes[_currentNodeIndex];
        ShowPlayerResponses(node);
    }

    private void ShowPlayerResponses(DialogueNode node)
    {
        playerPanel.SetActive(true);

        foreach (Transform child in responsesContainer)
            Destroy(child.gameObject);

        if (node.responses == null || node.responses.Length == 0)
        {
            // Нет вариантов — автоматически переходим к следующему узлу
            ShowNode(_currentNodeIndex + 1);
            return;
        }

        for (int i = 0; i < node.responses.Length; i++)
        {
            PlayerResponse response = node.responses[i];

            GameObject btnObj = Instantiate(responseButtonPrefab, responsesContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
            {
                // Показываем [ЗАКОНЧИТЬ] для завершающих реплик
                btnText.text = response.endsInterview ? "[ЗАКОНЧИТЬ]" : response.text;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                int capturedIndex = i;
                bool isCyclicNode = node.isCyclic;
                int currentNode = _currentNodeIndex;

                btn.onClick.AddListener(() =>
                {
                    OnResponseSelected(response, isCyclicNode, currentNode);
                });
            }
        }
    }

    private void OnResponseSelected(PlayerResponse response, bool isCyclicNode, int sourceNodeIndex)
    {
        AudioManager.Instance?.PlayButtonClick();
        HidePlayerResponses();

        if (response.endsInterview)
        {
            EndInterview();
            return;
        }

        if (isCyclicNode)
        {
            // Циклический узел: показываем промежуточную реплику НПС,
            // затем возвращаемся к тому же узлу
            if (response.nextNodeIndex >= 0 && response.nextNodeIndex < _data.nodes.Length)
            {
                // Показываем реакцию НПС (промежуточный узел)
                DialogueNode reactionNode = _data.nodes[response.nextNodeIndex];
                StartTypingThenReturn(reactionNode.npcLine, sourceNodeIndex);
            }
            else
            {
                // Нет промежуточного — сразу возвращаемся
                ShowNode(sourceNodeIndex);
            }
        }
        else
        {
            // Линейный: переходим к следующему узлу
            if (response.nextNodeIndex >= 0)
                ShowNode(response.nextNodeIndex);
            else
                ShowNode(_currentNodeIndex + 1);
        }
    }

    /// <summary>
    /// Печатает промежуточную реплику НПС, затем возвращается к циклическому узлу.
    /// </summary>
    private void StartTypingThenReturn(string line, int returnNodeIndex)
    {
        _fullLine = line;
        _skipTyping = false;
        _isTyping = true;

        if (_typeCoroutine != null)
            StopCoroutine(_typeCoroutine);

        _typeCoroutine = StartCoroutine(TypeTextThenReturn(line, returnNodeIndex));
    }

    private IEnumerator TypeTextThenReturn(string line, int returnNodeIndex)
    {
        npcLineText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping)
            {
                npcLineText.text = line;
                break;
            }

            npcLineText.text = line.Substring(0, i + 1);
            yield return new WaitForSeconds(typeSpeed);
        }

        _isTyping = false;

        // Ждём клика по панели НПС для продолжения
        yield return WaitForNpcPanelClick();

        // Возвращаемся к циклическому узлу
        ShowNode(returnNodeIndex);
    }

    private IEnumerator WaitForNpcPanelClick()
    {
        bool clicked = false;

        System.Action handler = () => { clicked = true; };
        npcPanelClickArea.onClick.AddListener(new UnityEngine.Events.UnityAction(handler));

        while (!clicked)
            yield return null;

        npcPanelClickArea.onClick.RemoveListener(new UnityEngine.Events.UnityAction(handler));
    }

    private void HidePlayerResponses()
    {
        playerPanel.SetActive(false);

        foreach (Transform child in responsesContainer)
            Destroy(child.gameObject);
    }

    private void EndInterview()
    {
        if (_typeCoroutine != null)
            StopCoroutine(_typeCoroutine);

        HidePlayerResponses();
        npcLineText.text = "";

        _onFinished?.Invoke(_data);
    }
}
