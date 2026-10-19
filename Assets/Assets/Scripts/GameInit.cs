using UnityEngine;

public class MainGameInitializer : MonoBehaviour
{
    private void Awake()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}