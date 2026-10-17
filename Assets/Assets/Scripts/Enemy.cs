using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseMaxHealth = 30f;
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private int scoreValue = 10;

    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public float MoveSpeed { get; private set; }

    public event Action<float, float> OnHealthChanged; // current, max

    private Rigidbody rb;
    private Transform target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        MaxHealth = baseMaxHealth;
        CurrentHealth = MaxHealth;
        MoveSpeed = baseMoveSpeed;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
        else Debug.LogWarning("Enemy: no object tagged 'Player' found.");
    }

    /// <summary>
    /// Speed and HP modifier. Call this right after spawning, e.g. Initialize(1.2f, 2f)
    /// = 20% faster and double HP. A spawner can scale this as waves progress.
    /// </summary>
    public void Initialize(float speedMultiplier, float hpMultiplier)
    {
        MoveSpeed = baseMoveSpeed * speedMultiplier;
        MaxHealth = baseMaxHealth * hpMultiplier;
        CurrentHealth = MaxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        Vector3 direction = target.position - rb.position;
        direction.y = 0f; // stay on the X-Z plane

        if (direction.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        rb.linearVelocity = direction.normalized * MoveSpeed;
    }

    // ---- Collision check: touching the enemy hurts the player ----
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(contactDamage); // PlayerHealth handles the cooldown
        }
    }

    // ---- Called by your player's attack/bullets later ----
    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0f) Die();
    }

    private void Die()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(scoreValue);

        Destroy(gameObject);
    }

    // Right-click the component header in the Inspector (Play mode) to test
    [ContextMenu("Debug: Take 10 Damage")]
    private void DebugDamage() => TakeDamage(10f);
}