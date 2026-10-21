using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneManagerGame : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject GameOverMenu;
    public PlayerHealth playerHealth;   // found automatically if left empty
    public string mainMenuSceneName = "MainMenu";

    public static bool IsPaused { get; private set; }
    public static bool IsGameOver { get; private set; }

    void Start()
    {
        // reset in case the previous run ended in game over
        IsPaused = false;
        IsGameOver = false;
        Time.timeScale = 1f;

        if (GameOverMenu != null) GameOverMenu.SetActive(false);

        // must happen while the player is still active (Find ignores inactive objects)
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.OnDied += ShowGameOver;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= ShowGameOver;
    }

    void Update()
    {
        if (IsGameOver) return; // no pausing once you've died

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void ShowGameOver()
    {
        IsGameOver = true;
        if (PauseMenu != null) PauseMenu.SetActive(false);
        GameOverMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        IsGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        IsGameOver = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}