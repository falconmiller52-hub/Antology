using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Экран диалога интервью.
/// Панель ответов игрока ВСЕГДА видна (пустая пока НПС говорит).
/// Hover на репликах: смена цвета + звук.
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

    [Header("Response Colors")]
    [SerializeField] private Color responseNormalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color responseHoverColor = new Color(1f, 1f, 0.7f, 1f);
    [SerializeField] private Color endResponseColor = new Color(0.8f, 0.3f, 0.3f, 1f);

    [Header("Response Hover SFX")]
    [SerializeField] private AudioClip responseHoverSFX;

    private InterviewData _data;
    private System.Action<InterviewData> _onFinished;
    private int _currentNodeIndex;
    private bool _isTyping;
    private bool _skipTyping;
    private Coroutine _typeCoroutine;

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

        if (node.intelKey != null)
            IntelManager.Instance?.CollectKey(node.intelKey);

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

    private void OnNpcPanelClicked()
    {
        if (_isTyping) _skipTyping = true;
    }

    private void OnTypingComplete()
    {
        ShowPlayerResponses(_data.nodes[_currentNodeIndex]);
    }

    private void ShowPlayerResponses(DialogueNode node)
    {
        ClearResponses();

        if (node.responses == null || node.responses.Length == 0)
        {
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
                btnText.text = response.endsInterview ? "[ЗАКОНЧИТЬ]" : response.text;
                btnText.color = response.endsInterview ? endResponseColor : responseNormalColor;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                bool isCyclic = node.isCyclic;
                int currentNode = _currentNodeIndex;
                PlayerResponse cap = response;
                btn.onClick.AddListener(() => OnResponseSelected(cap, isCyclic, currentNode));
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

    private void OnResponseSelected(PlayerResponse response, bool isCyclicNode, int sourceNodeIndex)
    {
        AudioManager.Instance?.PlayButtonClick();
        ClearResponses();

        if (response.endsInterview) { EndInterview(); return; }

        if (isCyclicNode)
        {
            if (response.nextNodeIndex >= 0 && response.nextNodeIndex < _data.nodes.Length)
            {
                DialogueNode reaction = _data.nodes[response.nextNodeIndex];
                if (reaction.intelKey != null)
                    IntelManager.Instance?.CollectKey(reaction.intelKey);
                StartTypingThenReturn(reaction.npcLine, sourceNodeIndex);
            }
            else
            {
                ShowNode(sourceNodeIndex);
            }
        }
        else
        {
            ShowNode(response.nextNodeIndex >= 0 ? response.nextNodeIndex : _currentNodeIndex + 1);
        }
    }

    private void StartTypingThenReturn(string line, int returnNodeIndex)
    {
        _skipTyping = false;
        _isTyping = true;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeTextThenReturn(line, returnNodeIndex));
    }

    private IEnumerator TypeTextThenReturn(string line, int returnNodeIndex)
    {
        npcLineText.text = "";
        _isTyping = true;
        _skipTyping = false;
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
        yield return WaitForNpcPanelClick();
        ShowNode(returnNodeIndex);
    }

    private IEnumerator WaitForNpcPanelClick()
    {
        bool clicked = false;
        System.Action handler = () => { clicked = true; };
        npcPanelClickArea.onClick.AddListener(new UnityEngine.Events.UnityAction(handler));
        while (!clicked) yield return null;
        npcPanelClickArea.onClick.RemoveListener(new UnityEngine.Events.UnityAction(handler));
    }

    private void ClearResponses()
    {
        foreach (Transform child in responsesContainer)
            Destroy(child.gameObject);
    }

    private void EndInterview()
    {
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        ClearResponses();
        npcLineText.text = "";
        _onFinished?.Invoke(_data);
    }
}
