using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Контроллер главного меню.
/// Вешается на GameObject "SCRIPTS/MainMenuController" в сцене MainMenu.
/// Кнопки Play и Quit вызывают методы этого скрипта через OnClick().
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplay";

    /// <summary>
    /// Привязывается к кнопке Play через Inspector → OnClick().
    /// </summary>
    public void OnPlayButton()
    {
        AudioManager.Instance?.PlayButtonClick();
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Привязывается к кнопке Quit через Inspector → OnClick().
    /// </summary>
    public void OnQuitButton()
    {
        AudioManager.Instance?.PlayButtonClick();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
