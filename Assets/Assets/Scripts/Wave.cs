using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// on an empty (invisible) GameObject in  scene. spawn point btw
public class WaveSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemyPrefab;   // drag your Enemy prefab here
    [SerializeField] private TMP_Text waveText;   // top-left UI text

    [Header("Wave Settings")]
    [SerializeField] private int baseEnemyCount = 5;      // enemies per wave in the first tier
    [SerializeField] private int wavesPerTier = 5;        // waves 1-5 = tier 0, waves 6-10 = tier 1, ...
    [SerializeField] private float startDelay = 2f;       // pause before wave 1
    [SerializeField] private float timeBetweenWaves = 3f; // pause after a wave is cleared
    [SerializeField] private float spawnInterval = 0.3f;  // gap between each enemy in a wave
    [SerializeField] private float spawnRadius = 2f;      // enemies appear randomly around the spawn point

    [Header("Optional difficulty scaling (uses Enemy.Initialize)")]
    [SerializeField] private float speedIncreasePerWave = 0.05f; // +5% speed per wave
    [SerializeField] private float hpIncreasePerWave = 0.10f;    // +10% HP per wave

    public int CurrentWave { get; private set; }

    private readonly List<Enemy> aliveEnemies = new List<Enemy>();

    private void Start()
    {
        UpdateUI();
        StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            CurrentWave++;
            UpdateUI();

            yield return SpawnWave(CurrentWave);

            // wait until every enemy from this wave is dead
            while (CountAlive() > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave(int wave)
    {
        int count = GetEnemyCount(wave);
        float speedMult = 1f + (wave - 1) * speedIncreasePerWave;
        float hpMult = 1f + (wave - 1) * hpIncreasePerWave;

        for (int i = 0; i < count; i++)
        {
            // random point around the spawn point on the X-Z plane (keeps the spawn point's height)
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + new Vector3(offset.x, 0f, offset.y);

            Enemy enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            enemy.Initialize(speedMult, hpMult);
            aliveEnemies.Add(enemy);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Waves 1-5 -> 5 enemies, 6-10 -> 10, 11-15 -> 20, ... (doubles every tier)
    private int GetEnemyCount(int wave)
    {
        int tier = (wave - 1) / wavesPerTier;
        tier = Mathf.Min(tier, 10); // safety cap so the number can't overflow
        return baseEnemyCount * (1 << tier);
    }

    private int CountAlive()
    {
        // destroyed enemies show up as null, so clear them out
        aliveEnemies.RemoveAll(e => e == null);
        return aliveEnemies.Count;
    }

    private void UpdateUI()
    {
        if (waveText != null)
            waveText.text = $"Wave: {CurrentWave}";
    }

    // Shows the spawn area in the Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}