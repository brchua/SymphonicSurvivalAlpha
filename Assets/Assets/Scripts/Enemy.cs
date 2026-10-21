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
    public event Action<Enemy> OnDied;                 // the wave controller listens to this

    private Rigidbody rb;
    private Transform target;
    private bool heightAligned;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false; // a kinematic body ignores velocity, so it would never move
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        MaxHealth = baseMaxHealth;
        CurrentHealth = MaxHealth;
        MoveSpeed = baseMoveSpeed;
    }

    private void Start()
    {
        FindTarget();
    }

    private void FindTarget()
    {
        // 1) try the "Player" tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            return;
        }

        // 2) fallback: find the object that has PlayerMovement (works even if the tag is missing)
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            target = pm.transform;
            Debug.LogWarning("Enemy: no object tagged 'Player', so I used the PlayerMovement object. " +
                             "Set the Player's Tag to 'Player' to fix this warning.");
        }
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
        if (target == null)
        {
            FindTarget(); // keep trying, in case the player isn't ready yet
            return;
        }

        // Enemy height is frozen, so on the first frame match the player's height once.
        // Otherwise an enemy spawned a bit too high or low slides past the player without touching.
        if (!heightAligned)
        {
            Vector3 aligned = rb.position;
            aligned.y = target.position.y;
            rb.position = aligned;
            transform.position = aligned;
            heightAligned = true;
        }

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
    private void OnCollisionEnter(Collision collision)
    {
        // TEMPORARY debug: tells you what the enemy touched. Delete once the HP bar works.
        bool found = collision.collider.GetComponentInParent<PlayerHealth>() != null;
        Debug.Log($"Enemy touched '{collision.collider.name}'. Has PlayerHealth: {found}");
    }

    private void OnCollisionStay(Collision collision)
    {
        HurtPlayer(collision.collider);
    }

    // Also works if the enemy's collider has "Is Trigger" checked
    private void OnTriggerStay(Collider other)
    {
        HurtPlayer(other);
    }

    private void HurtPlayer(Collider other)
    {
        // GetComponentInParent also finds PlayerHealth when the collider is on a child object
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(contactDamage); // PlayerHealth handles the cooldown
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

    // Fires however the enemy is removed (killed, or destroyed by something else),
    // so the wave controller's enemy count can never get stuck.
    private void OnDestroy()
    {
        OnDied?.Invoke(this);
    }

    // Right-click the component header in the Inspector (Play mode) to test
    [ContextMenu("Debug: Take 10 Damage")]
    private void DebugDamage() => TakeDamage(10f);
}