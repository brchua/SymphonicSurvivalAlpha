using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityTime = 1f; // grace period after getting hit

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    // UI (like a player HP bar) can subscribe to these
    public event Action<int, int> OnHealthChanged; // current, max
    public event Action OnDied;

    private float lastHitTime = -999f;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (CurrentHealth <= 0) return;
        if (Time.time < lastHitTime + invincibilityTime) return;

        lastHitTime = Time.time;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Debug.Log($"Player HP: {CurrentHealth}/{maxHealth}");   // TEMPORARY debug, delete later
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
        {
            OnDied?.Invoke();
            Debug.Log("Player died!");
            gameObject.SetActive(false); // replace with game over screen later
        }
    }
}