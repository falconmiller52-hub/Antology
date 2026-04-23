using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Главный контроллер ментальной карты сюжета.
///
/// ВАЖНО: ноды и нити кладутся в один родитель — mapArea — чтобы все
/// локальные координаты были согласованы. Z-порядок управляется через
/// SetAsFirstSibling для нитей (они рисуются под нодами).
///
/// Создание связей: клик ЛКМ по сокету → тянется линия за курсором →
/// клик ЛКМ по второму сокету (совместимому) → фиксация / красная вспышка.
/// ПКМ или Esc — отмена тянущейся нити.
/// </summary>
public class StoryMapUI : MonoBehaviour
{
    [Header("Map Area")]
    [Tooltip("Единый контейнер для нод и нитей. Все позиции считаются относительно него.")]
    [SerializeField] private RectTransform mapArea;

    [Header("Prefabs")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private GameObject connectionPrefab;

    [Header("UI Elements")]
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI topicTitleText;
    [SerializeField] private StoryMapSidePanel sidePanel;

    [Header("Viewport (optional)")]
    [Tooltip("Если назначен — скрипт подпишется на его OnPanStarted для скрытия sidePanel.")]
    [SerializeField] private StoryMapViewport viewport;

    private StoryTopic _currentTopic;
    private System.Action<StoryTopic> _onSubmitted;
    private System.Action _onReturnToCatalog;

    private List<StoryNodeUI> _nodes = new List<StoryNodeUI>();
    private List<StoryConnectionUI> _connections = new List<StoryConnectionUI>();

    private StoryConnectionUI _draggingConnection;
    private StorySocketUI _draggingFromSocket;

    public bool IsDraggingConnection => _draggingConnection != null;

    private bool _isActive;

    // Статический кэш состояния карты per-тема. Переживает закрытие/открытие
    // компьютера и повторный выбор темы. Сбрасывается только при OnSubmit
    // (тема отправлена) или ResetAll (новая игра).
    private static Dictionary<StoryTopic, StoryMapState> _stateCache
        = new Dictionary<StoryTopic, StoryMapState>();

    public static void ClearAllCachedStates() => _stateCache.Clear();

    private class StoryMapState
    {
        public Dictionary<StoryNode, Vector2> nodePositions = new Dictionary<StoryNode, Vector2>();
        // Пары (from, to) уже созданных связей.
        public List<(StoryNode from, StoryNode to)> connections = new List<(StoryNode, StoryNode)>();
    }

    public void Initialize(StoryTopic topic, System.Action<StoryTopic> onSubmitted, System.Action onReturnToCatalog = null)
    {
        _currentTopic = topic;
        _onSubmitted = onSubmitted;
        _onReturnToCatalog = onReturnToCatalog;
        _isActive = true;

        if (topicTitleText != null)
            topicTitleText.text = topic.topicTitle;

        submitButton.gameObject.SetActive(false);
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(OnSubmit);

        // Если на mapArea висит StoryMapViewport — настраиваем размер холста
        // из темы и сбрасываем зум/позицию к дефолту.
        StoryMapViewport viewport = mapArea.GetComponent<StoryMapViewport>();
        if (viewport != null)
        {
            if (topic.canvasSize != Vector2.zero)
                viewport.SetCanvasSize(topic.canvasSize);
            viewport.ResetView();
        }

        ClearAll();
        BuildNodes();
    }

    private void Update()
    {
        if (!_isActive) return;

        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            if (IsDraggingConnection)
                CancelDraggingConnection();
            else
            {
                SaveStateToCache();
                _isActive = false;
                _onReturnToCatalog?.Invoke();
            }
            return;
        }

        if (IsDraggingConnection && Mouse.current != null)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();

            Canvas canvas = mapArea.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                         ? canvas.worldCamera : null;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    mapArea, screenPos, cam, out localPoint))
            {
                _draggingConnection.UpdateDraggingEndpoint(localPoint);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
                CancelDraggingConnection();
        }
    }

    // ===== SidePanel =====

    private void OnNodeClicked(StoryNodeUI node)
    {
        if (sidePanel != null)
            sidePanel.ShowSingleNode(node.Node);
    }

    /// <summary>
    /// Вызывается StoryMapViewport когда игрок нажал ЛКМ на пустое место (не нода, не сокет).
    /// Скрывает sidePanel.
    /// </summary>
    public void OnEmptySpaceClicked()
    {
        if (sidePanel != null)
            sidePanel.Hide();
    }

    /// <summary>
    /// Показывает в sidePanel черновик: описания всех нод, входящих в текущую
    /// (возможно частичную) цепочку от cat 0 по возрастанию.
    /// </summary>
    private void ShowChainDraftInSidePanel()
    {
        if (sidePanel == null) return;

        var chainNodes = GetCurrentChainNodes();
        if (chainNodes.Count == 0)
        {
            sidePanel.Hide();
            return;
        }
        sidePanel.ShowChainDraft(chainNodes);
    }

    /// <summary>
    /// Ищет цепочку от ноды cat 0, проходя по существующим связям вверх по категориям,
    /// пока они есть. Может вернуть частичную цепочку (не обязательно полной длины).
    /// </summary>
    private List<StoryNode> GetCurrentChainNodes()
    {
        var result = new List<StoryNode>();

        // Берём первую cat=0 ноду, у которой есть исходящая связь.
        StoryNodeUI root = _nodes.Find(n => n.Node.category == 0 && HasAnyConnectionFrom(n));
        if (root == null) return result;

        result.Add(root.Node);
        StoryNodeUI current = root;

        while (true)
        {
            StoryConnectionUI next = _connections.Find(c => c.From == current);
            if (next == null) break;
            result.Add(next.To.Node);
            current = next.To;
        }
        return result;
    }

    // ===== Построение карты =====

    private void BuildNodes()
    {
        if (_currentTopic.nodePlacements == null) return;
        if (!ValidatePrefab(nodePrefab, "nodePrefab")) return;
        if (!ValidatePrefab(connectionPrefab, "connectionPrefab")) return;

        // Пробуем взять сохранённое состояние темы
        StoryMapState cached;
        _stateCache.TryGetValue(_currentTopic, out cached);

        foreach (var placement in _currentTopic.nodePlacements)
        {
            if (placement == null || placement.node == null) continue;

            GameObject obj = Instantiate(nodePrefab, mapArea);

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) { Destroy(obj); continue; }

            // Позиция: сначала пытаемся взять из кэша, иначе используем placement.
            Vector2 pos;
            if (cached != null && cached.nodePositions.TryGetValue(placement.node, out pos))
                rt.localPosition = pos;
            else
                rt.localPosition = placement.position;

            StoryNodeUI nodeUI = obj.GetComponent<StoryNodeUI>();
            if (nodeUI == null) { Destroy(obj); continue; }

            bool unlocked = IntelManager.Instance != null
                         && IntelManager.Instance.HasKey(placement.node.requiredIntelKey);

            nodeUI.Initialize(placement.node, unlocked, mapArea, this);
            nodeUI.OnPositionChanged += OnNodeMoved;
            nodeUI.OnClicked += OnNodeClicked;

            _nodes.Add(nodeUI);
        }

        // Восстанавливаем сохранённые связи
        if (cached != null && cached.connections.Count > 0)
        {
            foreach (var (fromData, toData) in cached.connections)
            {
                StoryNodeUI fromUI = _nodes.Find(n => n.Node == fromData);
                StoryNodeUI toUI = _nodes.Find(n => n.Node == toData);
                if (fromUI == null || toUI == null) continue;
                if (!fromUI.IsUnlocked || !toUI.IsUnlocked) continue;

                GameObject connObj = Instantiate(connectionPrefab, mapArea);
                connObj.transform.SetAsFirstSibling();
                StoryConnectionUI conn = connObj.GetComponent<StoryConnectionUI>();
                conn.InitializeComplete(fromUI, toUI, this);
                _connections.Add(conn);
            }
            EvaluateChain();
        }
    }

    private bool ValidatePrefab(GameObject prefab, string fieldName)
    {
        if (prefab == null)
        {
            Debug.LogError($"[StoryMap] Поле '{fieldName}' не назначено.");
            return false;
        }
        if (prefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError($"[StoryMap] Префаб '{prefab.name}' не имеет RectTransform.");
            return false;
        }
        return true;
    }

    private void OnNodeMoved(StoryNodeUI node)
    {
        foreach (var conn in _connections)
            if (conn.From == node || conn.To == node)
                conn.UpdateShape();

        if (_draggingConnection != null && _draggingConnection.From == node)
            _draggingConnection.UpdateShape();
    }

    // ===== Логика тянущейся нити =====

    public void BeginDraggingConnectionFrom(StorySocketUI socket)
    {
        if (IsDraggingConnection) CancelDraggingConnection();

        if (HasExistingConnectionOn(socket))
        {
            AudioManager.Instance?.PlayPCButton();
            return;
        }

        AudioManager.Instance?.PlayPCButton();
        _draggingFromSocket = socket;

        GameObject obj = Instantiate(connectionPrefab, mapArea);
        // Нити — под нодами
        obj.transform.SetAsFirstSibling();
        _draggingConnection = obj.GetComponent<StoryConnectionUI>();
        _draggingConnection.InitializeDragging(socket.Owner, socket.Type, this);
    }

    public void TryCompleteConnectionOn(StorySocketUI secondSocket)
    {
        if (_draggingConnection == null || _draggingFromSocket == null) return;

        StorySocketUI first = _draggingFromSocket;
        StorySocketUI second = secondSocket;

        if (first == second) { CancelDraggingConnection(); return; }
        if (first.Owner == second.Owner) { CancelDraggingConnection(); return; }

        int catA = first.Owner.Node.category;
        int catB = second.Owner.Node.category;

        StoryNodeUI fromNode, toNode;
        if (catB == catA + 1) { fromNode = first.Owner; toNode = second.Owner; }
        else if (catA == catB + 1) { fromNode = second.Owner; toNode = first.Owner; }
        else { StartCoroutine(FlashInvalidAndCancel()); return; }

        StorySocketUI fromSocket = (first.Owner == fromNode) ? first : second;
        StorySocketUI toSocket = (first.Owner == toNode) ? first : second;

        if (fromSocket.Type != SocketType.Output || toSocket.Type != SocketType.Input)
        {
            StartCoroutine(FlashInvalidAndCancel());
            return;
        }

        foreach (var c in _connections)
        {
            if (c.From == fromNode) { CancelDraggingConnection(); return; }
            if (c.To == toNode)     { CancelDraggingConnection(); return; }
        }

        AudioManager.Instance?.PlayPCButton();
        _draggingConnection.InitializeComplete(fromNode, toNode, this);
        _connections.Add(_draggingConnection);
        _draggingConnection = null;
        _draggingFromSocket = null;

        EvaluateChain();
        ShowChainDraftInSidePanel();
    }

    private System.Collections.IEnumerator FlashInvalidAndCancel()
    {
        if (_draggingConnection == null) yield break;
        _draggingConnection.SetState(StoryConnectionUI.ConnectionState.Invalid);
        yield return new WaitForSeconds(0.4f);
        CancelDraggingConnection();
    }

    public void CancelDraggingConnection()
    {
        if (_draggingConnection != null)
        {
            Destroy(_draggingConnection.gameObject);
            _draggingConnection = null;
        }
        _draggingFromSocket = null;
    }

    private bool HasExistingConnectionOn(StorySocketUI socket)
    {
        foreach (var c in _connections)
        {
            if (socket.Type == SocketType.Output && c.From == socket.Owner) return true;
            if (socket.Type == SocketType.Input && c.To == socket.Owner) return true;
        }
        return false;
    }

    private bool HasAnyConnectionFrom(StoryNodeUI node)
    {
        foreach (var c in _connections)
            if (c.From == node) return true;
        return false;
    }

    public void RemoveConnection(StoryConnectionUI conn)
    {
        AudioManager.Instance?.PlayPCButton();
        _connections.Remove(conn);
        Destroy(conn.gameObject);
        EvaluateChain();
        ShowChainDraftInSidePanel();
    }

    // ===== Валидация цепочки =====

    private void EvaluateChain()
    {
        List<StoryConnectionUI> chain = FindCompleteChain();
        bool complete = chain != null && chain.Count == _currentTopic.requiredChainLength - 1;

        Debug.Log($"[StoryMap] EvaluateChain: connections={_connections.Count}, " +
                  $"requiredLength={_currentTopic.requiredChainLength}, " +
                  $"foundChainLength={(chain != null ? chain.Count : 0)}, complete={complete}");

        foreach (var c in _connections)
            c.SetState(StoryConnectionUI.ConnectionState.Valid);

        if (complete)
        {
            foreach (var c in chain)
                c.SetState(StoryConnectionUI.ConnectionState.Complete);
            submitButton.gameObject.SetActive(true);
        }
        else
        {
            submitButton.gameObject.SetActive(false);
        }
    }

    private List<StoryConnectionUI> FindCompleteChain()
    {
        foreach (var root in _nodes)
        {
            if (root.Node.category != 0) continue;

            var result = new List<StoryConnectionUI>();
            StoryNodeUI current = root;
            int expectedNextCat = 1;
            bool ok = true;

            while (expectedNextCat < _currentTopic.requiredChainLength)
            {
                StoryConnectionUI next = _connections.Find(
                    c => c.From == current && c.To.Node.category == expectedNextCat);
                if (next == null) { ok = false; break; }
                result.Add(next);
                current = next.To;
                expectedNextCat++;
            }

            if (ok) return result;
        }
        return null;
    }

    private void OnSubmit()
    {
        List<StoryConnectionUI> chain = FindCompleteChain();
        if (chain == null) return;

        AudioManager.Instance?.PlayPCButton();

        var chainNodes = new List<StoryNode>();
        if (chain.Count > 0)
        {
            chainNodes.Add(chain[0].From.Node);
            foreach (var c in chain)
                chainNodes.Add(c.To.Node);
        }

        int fA = 0, fB = 0, fC = 0, fD = 0;
        var broadcastTexts = new List<string>();
        var perNodeDeltas = new List<FactionDelta>();
        foreach (var n in chainNodes)
        {
            fA += n.factionAPoints;
            fB += n.factionBPoints;
            fC += n.factionCPoints;
            fD += n.factionDPoints;
            if (!string.IsNullOrEmpty(n.description))
                broadcastTexts.Add(n.description);
            perNodeDeltas.Add(new FactionDelta
            {
                a = n.factionAPoints,
                b = n.factionBPoints,
                c = n.factionCPoints,
                d = n.factionDPoints
            });
        }

        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.RegisterStoryMap(
                broadcastTexts.ToArray(), fA, fB, fC, fD, perNodeDeltas.ToArray());

        TutorialManager.Instance?.OnTutorialEvent(TutorialEventType.StorySubmitted);

        // Тема отправлена — кэш больше не нужен
        if (_stateCache.ContainsKey(_currentTopic))
            _stateCache.Remove(_currentTopic);

        _isActive = false;
        _onSubmitted?.Invoke(_currentTopic);
    }

    private void SaveStateToCache()
    {
        if (_currentTopic == null) return;

        var state = new StoryMapState();
        foreach (var nodeUI in _nodes)
        {
            RectTransform rt = nodeUI.GetComponent<RectTransform>();
            state.nodePositions[nodeUI.Node] = rt.localPosition;
        }
        foreach (var conn in _connections)
        {
            state.connections.Add((conn.From.Node, conn.To.Node));
        }
        _stateCache[_currentTopic] = state;
    }

    private void ClearAll()
    {
        foreach (Transform child in mapArea)
        {
            if (child.GetComponent<StoryNodeUI>() != null || child.GetComponent<StoryConnectionUI>() != null)
                Destroy(child.gameObject);
        }
        _nodes.Clear();
        _connections.Clear();
        _draggingConnection = null;
        _draggingFromSocket = null;
        if (sidePanel != null) sidePanel.Hide();
    }
}

/// <summary>
/// Изменения очков фракций для одной ноды — используется для передачи
/// в Intermedia, чтобы показывать прыгающий текст "+1A" и т.п.
/// </summary>
[System.Serializable]
public struct FactionDelta
{
    public int a;
    public int b;
    public int c;
    public int d;
}
