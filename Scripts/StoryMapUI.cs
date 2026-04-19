using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Главный контроллер ментальной карты сюжета.
///
/// Отвечает за:
/// - Построение ячеек по nodePlacements темы.
/// - Проверку доступности ячеек по IntelKey.
/// - Логику создания/удаления связей.
/// - Валидацию цепочки и активацию кнопки "Отправить".
/// - Передачу результата в GameProgressManager.
///
/// Настройка:
/// 1. Canvas с панелью "StoryMapPanel".
/// 2. Внутри: RectTransform "MapArea" (большое рабочее поле) —
///    это canvasRect, относительно которого позиционируются ноды и линии.
/// 3. Контейнеры: nodesContainer (дочерний от MapArea), connectionsContainer (дочерний от MapArea).
/// 4. Префабы: nodePrefab (StoryNodeUI), connectionPrefab (StoryConnectionUI).
/// 5. Кнопка submitButton (внизу по центру), поле topicTitleText.
/// </summary>
public class StoryMapUI : MonoBehaviour
{
    [Header("Map Area")]
    [Tooltip("RectTransform рабочего поля карты. Внутри него размещаются ноды и линии.")]
    [SerializeField] private RectTransform mapArea;

    [Header("Containers")]
    [SerializeField] private RectTransform nodesContainer;
    [SerializeField] private RectTransform connectionsContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private GameObject connectionPrefab;

    [Header("UI Elements")]
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI topicTitleText;

    private StoryTopic _currentTopic;
    private System.Action<StoryTopic> _onSubmitted;
    private System.Action _onReturnToCatalog;

    private List<StoryNodeUI> _nodes = new List<StoryNodeUI>();
    private List<StoryConnectionUI> _connections = new List<StoryConnectionUI>();

    // Состояние создания связи: "вооружённый" сокет — ждёт второго клика.
    private StorySocketUI _armedSocket;

    private bool _isActive;

    /// <summary>
    /// Инициализирует карту для темы.
    /// </summary>
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

        ClearAll();
        BuildNodes();
    }

    private void Update()
    {
        if (!_isActive) return;

        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            // Отменяем вооружённый сокет, если есть
            if (_armedSocket != null)
            {
                _armedSocket.SetArmed(false);
                _armedSocket = null;
            }
            else
            {
                _isActive = false;
                _onReturnToCatalog?.Invoke();
            }
        }
    }

    private void BuildNodes()
    {
        if (_currentTopic.nodePlacements == null) return;

        foreach (var placement in _currentTopic.nodePlacements)
        {
            if (placement == null || placement.node == null) continue;

            GameObject obj = Instantiate(nodePrefab, nodesContainer);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.localPosition = placement.position;

            StoryNodeUI nodeUI = obj.GetComponent<StoryNodeUI>();
            bool unlocked = IntelManager.Instance != null
                         && IntelManager.Instance.HasKey(placement.node.requiredIntelKey);

            nodeUI.Initialize(placement.node, unlocked, mapArea, this);
            nodeUI.OnPositionChanged += OnNodeMoved;

            _nodes.Add(nodeUI);
        }
    }

    private void OnNodeMoved(StoryNodeUI node)
    {
        // Пересчитать все связи, связанные с этой нодой.
        foreach (var conn in _connections)
        {
            if (conn.From == node || conn.To == node)
                conn.UpdateShape();
        }
    }

    // ===== Логика создания связей =====

    public void OnSocketClicked(StorySocketUI socket)
    {
        AudioManager.Instance?.PlayPCButton();

        if (_armedSocket == null)
        {
            // Первый клик — только на Output
            if (socket.Type != SocketType.Output) return;

            _armedSocket = socket;
            socket.SetArmed(true);
            return;
        }

        // Клик по тому же сокету — отмена
        if (_armedSocket == socket)
        {
            socket.SetArmed(false);
            _armedSocket = null;
            return;
        }

        // Второй клик — ожидаем Input другой ноды
        if (socket.Type != SocketType.Input)
        {
            // Перепривязываем к новому Output, если кликнули на другой Output
            if (socket.Type == SocketType.Output)
            {
                _armedSocket.SetArmed(false);
                _armedSocket = socket;
                socket.SetArmed(true);
            }
            return;
        }

        StoryNodeUI from = _armedSocket.Owner;
        StoryNodeUI to = socket.Owner;

        _armedSocket.SetArmed(false);
        _armedSocket = null;

        TryCreateConnection(from, to);
    }

    private void TryCreateConnection(StoryNodeUI from, StoryNodeUI to)
    {
        // Нельзя соединять с самой собой
        if (from == to) return;

        // Правило категорий: cat N → cat N+1
        bool categoriesValid = (to.Node.category == from.Node.category + 1);

        // Проверка дублирования: у from.Output уже есть связь? (правило: одна связь с выхода)
        foreach (var c in _connections)
        {
            if (c.From == from) return; // у ноды from уже есть исходящая связь
            if (c.To == to) return;     // к ноде to уже ведёт входящая связь
        }

        if (!categoriesValid)
        {
            // Красная нить — создаём, показываем, через 1 секунду удаляем
            StartCoroutine(SpawnInvalidConnection(from, to));
            return;
        }

        GameObject obj = Instantiate(connectionPrefab, connectionsContainer);
        // Линии должны быть под нодами визуально:
        obj.transform.SetAsFirstSibling();

        StoryConnectionUI conn = obj.GetComponent<StoryConnectionUI>();
        conn.Initialize(from, to, this);
        _connections.Add(conn);

        EvaluateChain();
    }

    private System.Collections.IEnumerator SpawnInvalidConnection(StoryNodeUI from, StoryNodeUI to)
    {
        GameObject obj = Instantiate(connectionPrefab, connectionsContainer);
        obj.transform.SetAsFirstSibling();
        StoryConnectionUI conn = obj.GetComponent<StoryConnectionUI>();
        conn.Initialize(from, to, this);
        conn.SetState(StoryConnectionUI.ConnectionState.Invalid);

        yield return new WaitForSeconds(0.6f);

        if (conn != null) Destroy(conn.gameObject);
    }

    public void RemoveConnection(StoryConnectionUI conn)
    {
        AudioManager.Instance?.PlayPCButton();
        _connections.Remove(conn);
        Destroy(conn.gameObject);
        EvaluateChain();
    }

    // ===== Валидация цепочки =====

    /// <summary>
    /// Проверяет, собрана ли валидная цепочка нужной длины (0→1→...).
    /// Если да — красит связи в зелёный и показывает кнопку "Отправить".
    /// </summary>
    private void EvaluateChain()
    {
        List<StoryConnectionUI> chain = FindCompleteChain();
        bool complete = chain != null && chain.Count == _currentTopic.requiredChainLength - 1;

        // Сбрасываем состояние всех связей
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

    /// <summary>
    /// Ищет цепочку cat 0 → cat 1 → ... → cat (requiredChainLength-1).
    /// Возвращает список связей в порядке цепочки, или null.
    /// </summary>
    private List<StoryConnectionUI> FindCompleteChain()
    {
        // Стартуем с ноды категории 0
        StoryNodeUI root = _nodes.Find(n => n.Node.category == 0 && HasAnyConnectionFrom(n));
        if (root == null) return null;

        var result = new List<StoryConnectionUI>();
        StoryNodeUI current = root;
        int expectedNextCat = 1;

        while (expectedNextCat < _currentTopic.requiredChainLength)
        {
            StoryConnectionUI next = _connections.Find(
                c => c.From == current && c.To.Node.category == expectedNextCat);
            if (next == null) return null;

            result.Add(next);
            current = next.To;
            expectedNextCat++;
        }

        return result;
    }

    private bool HasAnyConnectionFrom(StoryNodeUI node)
    {
        foreach (var c in _connections)
            if (c.From == node) return true;
        return false;
    }

    // ===== Отправка сюжета =====

    private void OnSubmit()
    {
        List<StoryConnectionUI> chain = FindCompleteChain();
        if (chain == null) return;

        AudioManager.Instance?.PlayPCButton();

        // Собираем ноды цепочки по порядку: root → through each connection.To
        var chainNodes = new List<StoryNode>();
        if (chain.Count > 0)
        {
            chainNodes.Add(chain[0].From.Node);
            foreach (var c in chain)
                chainNodes.Add(c.To.Node);
        }

        // Подсчёт очков и сбор текстов для эфира
        int fA = 0, fB = 0, fC = 0, fD = 0;
        var broadcastTexts = new List<string>();
        foreach (var n in chainNodes)
        {
            fA += n.factionAPoints;
            fB += n.factionBPoints;
            fC += n.factionCPoints;
            fD += n.factionDPoints;
            if (!string.IsNullOrEmpty(n.broadcastText))
                broadcastTexts.Add(n.broadcastText);
        }

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.RegisterStoryMap(
                broadcastTexts.ToArray(),
                fA, fB, fC, fD
            );
        }

        TutorialManager.Instance?.OnTutorialEvent(TutorialEventType.StorySubmitted);

        _isActive = false;
        _onSubmitted?.Invoke(_currentTopic);
    }

    private void ClearAll()
    {
        foreach (Transform child in nodesContainer) Destroy(child.gameObject);
        foreach (Transform child in connectionsContainer) Destroy(child.gameObject);
        _nodes.Clear();
        _connections.Clear();
        _armedSocket = null;
    }
}
