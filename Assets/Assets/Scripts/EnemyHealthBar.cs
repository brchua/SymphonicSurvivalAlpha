using UnityEngine;
using UnityEngine.UI;

// Put this on a World Space Canvas that is a child of the enemy.
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage; // Image Type = Filled, Fill Method = Horizontal

    private Enemy enemy;

    private void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        enemy.OnHealthChanged += UpdateBar;
        UpdateBar(enemy.CurrentHealth, enemy.MaxHealth);
    }

    private void OnDestroy()
    {
        if (enemy != null) enemy.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(float current, float max)
    {
        fillImage.fillAmount = current / max;
    }

    private void LateUpdate()
    {
        // keep the bar upright even if the enemy rotates
        transform.rotation = Quaternion.identity;
    }
}