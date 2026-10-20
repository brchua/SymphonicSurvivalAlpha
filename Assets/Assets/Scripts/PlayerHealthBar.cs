using UnityEngine;

// Shrinks a red rectangle as the player loses HP.
// Put this on the HP bar's background object (NOT on the player).
public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform fill;          // the red rectangle (its Pivot X must be 0)
    [SerializeField] private PlayerHealth playerHealth;   // found automatically if left empty

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealthBar: no PlayerHealth found in the scene.");
            return;
        }

        playerHealth.OnHealthChanged += UpdateBar;
        UpdateBar(playerHealth.CurrentHealth, playerHealth.MaxHealth); // start full
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(int current, int max)
    {
        float percent = Mathf.Clamp01((float)current / max);
        fill.localScale = new Vector3(percent, 1f, 1f); // shrinks toward the left edge
    }
}