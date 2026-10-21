using System.Collections;
using TMPro;
using UnityEngine;

// Wave event controller.
// Spawn locations are other empty
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
    [SerializeField] private float spawnInterval = 0.3f;
    [SerializeField] private float spawnJitter = 1f;
    [SerializeField] private float spawnHeightOffset = 0.5f;

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
    private float groundHeight;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("WaveSpawner: Enemy Prefab is not assigned!");
            return;
        }

        // stop spawning when the player dies
        playerHealth = FindAnyObjectByType<PlayerHealth>();
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

        float speedMult =
            Mathf.Pow(1f + speedIncreasePerTier, tier);

        float hpMult =
            1f + hpIncreasePerTier * tier;

        for (int i = 0; i < count; i++)
        {
            Transform spawnPoint = GetSpawnPoint();

            Vector2 randomOffset =
                Random.insideUnitCircle * spawnJitter;

            Vector3 spawnPosition =
                spawnPoint.position +
                new Vector3(randomOffset.x, 0f, randomOffset.y);

            Enemy enemy = Instantiate(
                enemyPrefab,
                spawnPosition,
                Quaternion.identity);

            PlaceEnemyOnGround(enemy, spawnPoint.position.y);

            enemy.Initialize(speedMult, hpMult);
            enemy.OnDied += HandleEnemyDied;
            aliveCount++;

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[
                Random.Range(0, spawnPoints.Length)];
        }

        return transform;
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

    private void PlaceEnemyOnGround(Enemy enemy, float yPos)
    {
        Collider[] colliders =
            enemy.GetComponentsInChildren<Collider>();

        float lowestPoint = float.PositiveInfinity;

        foreach (Collider enemyCollider in colliders)
        {
            if (enemyCollider.isTrigger)
            {
                continue;
            }

            lowestPoint = Mathf.Min(
                lowestPoint,
                enemyCollider.bounds.min.y);
        }

        if (float.IsPositiveInfinity(lowestPoint))
        {
            Debug.LogWarning(
                $"Enemy '{enemy.name}' has no non-trigger collider.",
                enemy);

            return;
        }

        float heightCorrection =
            groundHeight - lowestPoint + 0.01f;

        Vector3 position = enemy.transform.position;
        position.y += heightCorrection;
        enemy.transform.position = position;
    }
}