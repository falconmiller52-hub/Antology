using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Синглтон для управления всеми звуками игры.
/// 3 AudioSource: музыка, эмбиент, SFX.
/// Музыка переключается автоматически по сценам.
/// Эмбиент играет только в Gameplay.
///
/// Настройка:
/// 1. GameObject "AudioManager" в MainMenu с DontDestroyOnLoad.
/// 2. Три дочерних AudioSource (или на том же объекте).
/// 3. Назначьте все клипы в инспекторе.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip intermediaMusic;
    [SerializeField] private AudioClip endingMusic;

    [Header("Ambient")]
    [SerializeField] private AudioClip gameplayAmbient;

    [Header("SFX — UI General")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip pauseMenuSFX;

    [Header("SFX — Letters & Newspapers")]
    [SerializeField] private AudioClip letterOpenSFX;
    [SerializeField] private AudioClip letterCloseSFX;
    [SerializeField] private AudioClip newspaperOpenSFX;
    [SerializeField] private AudioClip newspaperCloseSFX;

    [Header("SFX — Radio")]
    [SerializeField] private AudioClip radioTuningSFX;
    [SerializeField] private AudioClip radioStaticSFX;
    [SerializeField] private AudioClip radioVoiceSFX;

    [Header("SFX — Computer")]
    [SerializeField] private AudioClip keyboardTypingSFX;
    [SerializeField] private AudioClip pcButtonSFX;
    [SerializeField] private AudioClip pcPowerSFX;

    [Header("SFX — Equipment")]
    [SerializeField] private AudioClip equipmentActivateSFX;

    [Header("SFX — Broadcast")]
    [SerializeField] private AudioClip sirenSFX;

    [Header("Volume Settings")]
    [SerializeField] private float normalMusicVolume = 0.8f;
    [SerializeField] private float pausedMusicVolume = 0.25f;
    [SerializeField] private float ambientVolume = 0.5f;
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Музыка по сценам
        AudioClip targetClip = scene.name switch
        {
            "MainMenu" => mainMenuMusic,
            "Gameplay" => gameplayMusic,
            "Intermedia" => intermediaMusic,
            "EndingA" => endingMusic,
            "EndingB" => endingMusic,
            _ => null
        };

        if (targetClip != null && musicSource.clip != targetClip)
        {
            musicSource.clip = targetClip;
            musicSource.volume = normalMusicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
        else if (targetClip == null)
        {
            musicSource.Stop();
        }

        SetMusicVolume(normalMusicVolume);

        // Эмбиент только в Gameplay
        if (scene.name == "Gameplay" && gameplayAmbient != null)
        {
            ambientSource.clip = gameplayAmbient;
            ambientSource.volume = ambientVolume;
            ambientSource.loop = true;
            ambientSource.Play();
        }
        else
        {
            ambientSource.Stop();
        }
    }

    // ===== UI General =====

    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlayPauseMenu()
    {
        PlaySFX(pauseMenuSFX);
    }

    // ===== Letters & Newspapers =====

    public void PlayLetterOpen()
    {
        PlaySFX(letterOpenSFX);
    }

    public void PlayLetterClose()
    {
        PlaySFX(letterCloseSFX);
    }

    public void PlayNewspaperOpen()
    {
        PlaySFX(newspaperOpenSFX);
    }

    public void PlayNewspaperClose()
    {
        PlaySFX(newspaperCloseSFX);
    }

    // ===== Radio =====

    public void PlayRadioTuning()
    {
        PlaySFX(radioTuningSFX);
    }

    /// <summary>
    /// Возвращает клип помех для RadioReceiver (назначается на AudioSource).
    /// </summary>
    public AudioClip GetRadioStaticClip() => radioStaticSFX;

    /// <summary>
    /// Возвращает клип голоса для RadioReceiver (назначается на AudioSource).
    /// </summary>
    public AudioClip GetRadioVoiceClip() => radioVoiceSFX;

    // ===== Computer =====

    public void PlayKeyboardTyping()
    {
        PlaySFX(keyboardTypingSFX);
    }

    /// <summary>
    /// Звук кнопок в меню ПК. Используется ВМЕСТО PlayButtonClick внутри компьютера.
    /// </summary>
    public void PlayPCButton()
    {
        PlaySFX(pcButtonSFX);
    }

    public void PlayPCPower()
    {
        PlaySFX(pcPowerSFX);
    }

    // ===== Equipment =====

    public void PlayEquipmentActivate()
    {
        PlaySFX(equipmentActivateSFX);
    }

    // ===== Broadcast =====

    public void PlaySiren()
    {
        PlaySFX(sirenSFX);
    }

    // ===== Music Control =====

    public void DimMusic()
    {
        SetMusicVolume(pausedMusicVolume);
    }

    public void RestoreMusic()
    {
        SetMusicVolume(normalMusicVolume);
    }

    public void DimAmbient()
    {
        if (ambientSource.isPlaying)
            ambientSource.volume = ambientVolume * 0.3f;
    }

    public void RestoreAmbient()
    {
        if (ambientSource.isPlaying)
            ambientSource.volume = ambientVolume;
    }

    private void SetMusicVolume(float volume)
    {
        _targetVolume = volume;
        _isFading = true;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }
}
