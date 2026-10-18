using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneManagerGame : MonoBehaviour
{
    public GameObject PauseMenu;
    public string mainMenuSceneName = "MainMenu";

    public static bool IsPaused { get; private set; }

    void Update()
    {
        // New Input System:
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        PauseMenu.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;
    }

    public void Resume()
    {
        PauseMenu.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // important, or the next scene stays frozen
        IsPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}