using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Экран диалога интервью.
/// Два типа узлов:
///  - Linear: NPC говорит → клик по панели переходит на nextNodeIndex.
///  - Choice: NPC говорит → показываются варианты ответов.
///
/// Intel keys автоматически собираются при показе реплик НПС.
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
    [SerializeField] private int charsPerBlip = 2;
    [SerializeField] private AudioSource blipSource;

    [Header("Linear Node Hint")]
    [Tooltip("Можно опционально показывать подсказку '▼ клик для продолжения' на Linear-узлах")]
    [SerializeField] private GameObject linearContinueHint;

    [Header("Response Colors")]
    [SerializeField] private Color responseNormalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color responseHoverColor = new Color(1f, 1f, 0.7f, 1f);
    [SerializeField] private Color endResponseColor = new Color(0.8f, 0.3f, 0.3f, 1f);

    [Header("Response Hover SFX")]
    [SerializeField] private AudioClip responseHoverSFX;

    [Header("Intel Highlight")]
    [SerializeField] private Color normalNpcLineColor = Color.white;
    [SerializeField] private Color intelNpcLineColor = new Color(1f, 0.6f, 0.6f, 1f);

    private InterviewData _data;
    private System.Action<InterviewData> _onFinished;
    private int _currentNodeIndex;
    private bool _isTyping;
    private bool _skipTyping;
    private bool _waitingForLinearContinue;
    private Coroutine _typeCoroutine;

    private void Awake()
    {
        if (blipSource == null)
        {
            blipSource = gameObject.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
        }
    }

    public void Initialize(InterviewData data, System.Action<InterviewData> onFinished)
    {
        _data = data;
        _onFinished = onFinished;
        _currentNodeIndex = 0;

        if (npcIcon != null && data.npcIcon != null)
            npcIcon.sprite = data.npcIcon;
        if (npcNameText != null)
            npcNameText.text = data.npcName;

        npcPanelClickArea.onClick.RemoveAllListeners();
        npcPanelClickArea.onClick.AddListener(OnNpcPanelClicked);

        playerPanel.SetActive(true);
        ClearResponses();
        HideLinearHint();
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

        ClearResponses();
        HideLinearHint();

        // Подсветка реплик с intel
        bool hasIntel = node.intelKey != null;
        npcLineText.color = hasIntel ? intelNpcLineColor : normalNpcLineColor;

        if (hasIntel)
            IntelManager.Instance?.CollectKey(node.intelKey);

        _waitingForLinearContinue = false;
        StartTyping(node.npcLine);
    }

    private void StartTyping(string line)
    {
        _skipTyping = false;
        _isTyping = true;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string line)
    {
        npcLineText.text = "";
        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTyping) { npcLineText.text = line; break; }
            npcLineText.text = line.Substring(0, i + 1);
            if (_data.voiceBlip != null && blipSource != null
                && !char.IsWhiteSpace(line[i]) && i % charsPerBlip == 0)
            {
                blipSource.pitch = _data.voicePitch + Random.Range(-0.05f, 0.05f);
                blipSource.PlayOneShot(_data.voiceBlip);
            }
            yield return new WaitForSeconds(typeSpeed);
        }
        _isTyping = false;
        OnTypingComplete();
    }

    private void OnTypingComplete()
    {
        DialogueNode node = _data.nodes[_currentNodeIndex];

        if (node.type == DialogueNodeType.Linear)
        {
            // Ждём клика по NPC-панели, затем идём на nextNodeIndex
            _waitingForLinearContinue = true;
            ShowLinearHint();
        }
        else // Choice
        {
            ShowPlayerResponses(node);
        }
    }

    private void OnNpcPanelClicked()
    {
        // 1) Пропуск печати.
        if (_isTyping)
        {
            _skipTyping = true;
            return;
        }

        // 2) Продолжение Linear-узла.
        if (_waitingForLinearContinue)
        {
            _waitingForLinearContinue = false;
            HideLinearHint();

            AudioManager.Instance?.PlayButtonClick();

            DialogueNode node = _data.nodes[_currentNodeIndex];
            if (node.nextNodeIndex < 0)
                EndInterview();
            else
                ShowNode(node.nextNodeIndex);
        }
    }

    private void ShowPlayerResponses(DialogueNode node)
    {
        ClearResponses();

        if (node.responses == null || node.responses.Length == 0)
        {
            Debug.LogWarning($"[Interview] Choice-узел {_currentNodeIndex} не имеет ответов. " +
                             $"Пометьте его как Linear или добавьте responses. Завершаю интервью.");
            EndInterview();
            return;
        }

        for (int i = 0; i < node.responses.Length; i++)
        {
            PlayerResponse response = node.responses[i];
            GameObject btnObj = Instantiate(responseButtonPrefab, responsesContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
            {
                btnText.text = response.endsInterview ? "[ЗАКОНЧИТЬ]" : response.text;
                btnText.color = response.endsInterview ? endResponseColor : responseNormalColor;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                PlayerResponse cap = response;
                btn.onClick.AddListener(() => OnResponseSelected(cap));
            }

            if (!response.endsInterview)
                AddResponseHoverEffect(btnObj, btnText);
        }
    }

    private void AddResponseHoverEffect(GameObject btnObj, TextMeshProUGUI text)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((_) =>
        {
            if (text != null) text.color = responseHoverColor;
            if (responseHoverSFX != null) AudioManager.Instance?.PlaySFXDirect(responseHoverSFX);
        });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((_) =>
        {
            if (text != null) text.color = responseNormalColor;
        });
        trigger.triggers.Add(exit);
    }

    private void OnResponseSelected(PlayerResponse response)
    {
        AudioManager.Instance?.PlayButtonClick();
        ClearResponses();

        if (response.endsInterview || response.nextNodeIndex < 0)
        {
            EndInterview();
            return;
        }

        ShowNode(response.nextNodeIndex);
    }

    private void ClearResponses()
    {
        foreach (Transform child in responsesContainer)
            Destroy(child.gameObject);
    }

    private void ShowLinearHint()
    {
        if (linearContinueHint != null)
            linearContinueHint.SetActive(true);
    }

    private void HideLinearHint()
    {
        if (linearContinueHint != null)
            linearContinueHint.SetActive(false);
    }

    private void EndInterview()
    {
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        ClearResponses();
        HideLinearHint();
        _waitingForLinearContinue = false;
        npcLineText.text = "";
        _onFinished?.Invoke(_data);
    }
}
