using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Синглтон для управления музыкой и звуками.
/// Размещается на отдельном GameObject в самой первой сцене (MainMenu).
/// Имеет два AudioSource: один для музыки, другой для SFX.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("SFX")]
    [SerializeField] private AudioClip buttonClickSFX;

    [Header("Settings")]
    [SerializeField] private float normalMusicVolume = 0.8f;
    [SerializeField] private float pausedMusicVolume = 0.25f;
    [SerializeField] private float fadeDuration = 0.5f;

    private float _targetVolume;
    private bool _isFading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!_isFading) return;

        musicSource.volume = Mathf.MoveTowards(
            musicSource.volume,
            _targetVolume,
            Time.unscaledDeltaTime / fadeDuration
        );

        if (Mathf.Approximately(musicSource.volume, _targetVolume))
            _isFading = false;
    }

    /// <summary>
    /// Вызывается автоматически при загрузке любой сцены.
    /// Переключает музыку в зависимости от имени сцены.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip targetClip = scene.name switch
        {
            "MainMenu" => mainMenuMusic,
            "Gameplay" => gameplayMusic,
            _ => null
        };

        if (targetClip != null && musicSource.clip != targetClip)
        {
            musicSource.clip = targetClip;
            musicSource.volume = normalMusicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }

        // Сбрасываем громкость на нормальную при смене сцены
        SetMusicVolume(normalMusicVolume);
    }

    /// <summary>
    /// Воспроизводит звук нажатия кнопки.
    /// </summary>
    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
            sfxSource.PlayOneShot(buttonClickSFX);
    }

    /// <summary>
    /// Приглушает музыку (для паузы).
    /// </summary>
    public void DimMusic()
    {
        SetMusicVolume(pausedMusicVolume);
    }

    /// <summary>
    /// Восстанавливает нормальную громкость музыки.
    /// </summary>
    public void RestoreMusic()
    {
        SetMusicVolume(normalMusicVolume);
    }

    private void SetMusicVolume(float volume)
    {
        _targetVolume = volume;
        _isFading = true;
    }
}
