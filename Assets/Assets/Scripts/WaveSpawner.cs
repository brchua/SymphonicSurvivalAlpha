using System.Collections;
using UnityEngine;
using TMPro;

// Wave event controller.
// Put this on any GameObject (e.g. an empty "WaveManager"). Spawn locations are other empty
// GameObjects that you drag into the "Spawn Points" list.
public class WaveSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemyPrefab;      // the Enemy PREFAB from the Project window
    [SerializeField] private TMP_Text waveText;      // top-left UI text
    [Tooltip("Empty GameObjects placed in the scene. If left empty, this object's own position is used.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Wave Rules")]
    [SerializeField] private int baseEnemyCount = 5;       // enemies in waves 1-3
    [SerializeField] private int wavesPerTier = 3;         // every 3 waves = a new tier
    [SerializeField] private int maxEnemiesPerWave = 100;  // safety cap, doubling gets huge fast
    [SerializeField] private float startDelay = 2f;        // pause before wave 1
    [SerializeField] private float timeBetweenWaves = 3f;  // pause after a wave is cleared
    [SerializeField] private float spawnInterval = 0.3f;   // gap between enemies in a wave
    [SerializeField] private float spawnJitter = 1f;       // random offset around a spawn point

    [Header("Scaling (applied once per tier)")]
    [SerializeField] private float speedIncreasePerTier = 0.20f; // +20% of the CURRENT speed (compounds)
    [SerializeField] private float hpIncreasePerTier = 0.25f;    // added to the HP multiplier

    public int CurrentWave { get; private set; }
    public int WavesSurvived { get; private set; }   // waves fully cleared (useful for the score system)

    // Other scripts (score, music, UI) can subscribe to these
    public event System.Action<int> OnWaveStarted;
    public event System.Action<int> OnWaveCleared;

    private int aliveCount;
    private PlayerHealth playerHealth;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("WaveSpawner: Enemy Prefab is not assigned!");
            return;
        }

        // stop spawning when the player dies
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnDied += HandlePlayerDied;

        UpdateUI();
        StartCoroutine(WaveLoop());
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        StopAllCoroutines();
    }

    private IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            CurrentWave++;
            UpdateUI();
            OnWaveStarted?.Invoke(CurrentWave);

            yield return SpawnWave(CurrentWave);

            // next wave only comes when no enemies remain on the field
            while (aliveCount > 0)
                yield return null;

            WavesSurvived++;
            OnWaveCleared?.Invoke(CurrentWave);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave(int wave)
    {
        int tier = GetTier(wave);
        int count = GetEnemyCount(tier);
        float speedMult = Mathf.Pow(1f + speedIncreasePerTier, tier); // x1.2 each tier, compounding
        float hpMult = 1f + hpIncreasePerTier * tier;                 // +0.25 each tier

        for (int i = 0; i < count; i++)
        {
            Enemy enemy = Instantiate(enemyPrefab, GetSpawnPosition(), Quaternion.identity);
            enemy.Initialize(speedMult, hpMult);

            aliveCount++;
            enemy.OnDied += HandleEnemyDied;

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    // Waves 1-3 = tier 0, waves 4-6 = tier 1, waves 7-9 = tier 2, ...
    private int GetTier(int wave)
    {
        return (wave - 1) / wavesPerTier;
    }

    // 5, 10, 20, 40, ... (doubles every tier), capped
    private int GetEnemyCount(int tier)
    {
        float count = baseEnemyCount * Mathf.Pow(2f, tier);
        return Mathf.Min(Mathf.RoundToInt(count), maxEnemiesPerWave);
    }

    private Vector3 GetSpawnPosition()
    {
        Transform point = transform;
        if (spawnPoints != null && spawnPoints.Length > 0)
            point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Vector2 jitter = Random.insideUnitCircle * spawnJitter;
        return point.position + new Vector3(jitter.x, 0f, jitter.y); // jitter on the X-Z plane
    }

    private void UpdateUI()
    {
        if (waveText != null)
            waveText.text = $"Wave: {CurrentWave}";
    }

    // Draws the spawn points in the Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (Transform p in spawnPoints)
                if (p != null) Gizmos.DrawWireSphere(p.position, Mathf.Max(spawnJitter, 0.3f));
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(spawnJitter, 0.3f));
        }
    }
}