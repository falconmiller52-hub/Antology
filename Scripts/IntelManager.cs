using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Синглтон для отслеживания собранных ключей разведки.
/// Живёт между сценами (DontDestroyOnLoad).
///
/// Настройка:
/// Создайте GameObject "IntelManager" в MainMenu рядом с GameProgressManager.
/// </summary>
public class IntelManager : MonoBehaviour
{
    public static IntelManager Instance { get; private set; }

    [Header("SFX")]
    [SerializeField] private AudioClip markingSFX;

    private HashSet<IntelKey> _collectedKeys = new HashSet<IntelKey>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Маркирует ключ как собранный. Проигрывает звук маркирования.
    /// </summary>
    public void CollectKey(IntelKey key)
    {
        if (key == null) return;
        if (_collectedKeys.Add(key))
        {
            Debug.Log($"[Intel] Collected: {key.keyName}");
            if (markingSFX != null)
                AudioManager.Instance?.PlaySFXDirect(markingSFX);

            TutorialManager.Instance?.OnTutorialEvent(TutorialEventType.IntelCollected);
        }
    }

    /// <summary>
    /// Проверяет, собран ли ключ.
    /// </summary>
    public bool HasKey(IntelKey key)
    {
        if (key == null) return true; // Нет ключа = всегда доступно
        return _collectedKeys.Contains(key);
    }

    /// <summary>
    /// Сброс при новой игре.
    /// </summary>
    public void ResetAll()
    {
        _collectedKeys.Clear();
    }
}
