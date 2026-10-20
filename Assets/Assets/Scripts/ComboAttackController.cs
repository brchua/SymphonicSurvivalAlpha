using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ComboAttackController : MonoBehaviour
{
    [System.Serializable]
    public class Combo
    {
        public string name;
        public Key[] keys;
        public string animatorTrigger;
        public AudioClip guitarAtkSfx;
        public AudioClip fluteAtkSfx;
        public AudioClip drumsAtkSfx;
    }

    [SerializeField] private Animator animator;
    [SerializeField] private BeatBar beatBar;   // metronome check. Leave empty to play without the beat bar.
    [SerializeField] private float sequenceTimeLimit = 5f;
    [SerializeField] private AudioSource sfxSourceGuitar;
    [SerializeField] private AudioSource sfxSourceFlute;
    [SerializeField] private AudioSource sfxSourceDrums;
    [SerializeField] private float sfxDuration = 3f;
    [SerializeField] private float sfxFadeOut = 0.2f;
    private readonly Dictionary<AudioSource, Coroutine> sfxRoutines = new Dictionary<AudioSource, Coroutine>();

    [Header("Drums - AOE blast")]
    [SerializeField] private float aoeRadius = 4f;
    [SerializeField] private float aoeDamage = 20f;
    [SerializeField] private float aoeDelay = 0.25f;          // wait so the damage lands when the drum animation hits
    [SerializeField] private GameObject aoeEffectPrefab;      // optional visual, spawned at the player

    [Header("Flute - Speed boost")]
    [SerializeField] private PlayerMovement playerMovement;   // auto-found if left empty
    [SerializeField] private float speedBoostMultiplier = 1.5f; // 1.5 = 50% faster
    [SerializeField] private float speedBoostDuration = 4f;   // seconds

    [Header("Guitar - Homing projectiles")]
    [SerializeField] private HomingProjectile projectilePrefab;
    [SerializeField] private int projectileCount = 4;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float shotDelay = 0.2f;          // wait so shots leave when the guitar animation hits
    [SerializeField] private float shotInterval = 0.1f;       // gap between the 4 shots
    [SerializeField] private float spreadAngle = 25f;         // fan-out angle between shots (they curve back in)
    [SerializeField] private float targetRange = 25f;         // enemies farther than this are ignored
    [SerializeField] private Transform firePoint;             // optional, defaults to the player's position

    [SerializeField]
    private List<Combo> combos = new List<Combo>
    {
        new Combo { name = "Guitar", keys = new[] { Key.Z, Key.X, Key.C, Key.V }, animatorTrigger = "AttackGuitar" },
        new Combo { name = "Flute",  keys = new[] { Key.C, Key.V, Key.Z, Key.X }, animatorTrigger = "AttackFlute" },
        new Combo { name = "Drums",  keys = new[] { Key.X, Key.C, Key.Z, Key.V }, animatorTrigger = "AttackDrums" }
    };

    private readonly List<Key> buffer = new List<Key>();
    private readonly HashSet<Key> comboKeys = new HashSet<Key>();
    private float timer;

    private void Awake()
    {
        RebuildKeySet();

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    private void OnValidate()
    {
        RebuildKeySet(); // keeps the key set in sync if you edit combos in the Inspector
    }

    private void RebuildKeySet()
    {
        comboKeys.Clear();
        foreach (var combo in combos)
            foreach (var key in combo.keys)
                comboKeys.Add(key);
    }

    public bool IsAttacking { get; private set; }
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Track the current attack animation
        var info = animator.GetCurrentAnimatorStateInfo(0);
        IsAttacking = info.IsTag("Attack");
        if (IsAttacking) return; // ignore combo input mid-attack

        // 5-second window starts on the first correct key
        if (buffer.Count > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) FailSequence("Too slow");
        }

        foreach (var key in comboKeys)
        {
            if (!keyboard[key].wasPressedThisFrame) continue;

            if (!IsInputAllowed())
            {
                // missed the beat -> the container (buffer) clears
                FailSequence("Off beat");
                continue;
            }

            HandleKey(key);
        }
    }

    private void HandleKey(Key key)
    {
        buffer.Add(key);

        if (!IsPrefixOfAnyCombo(buffer))
        {
            // Wrong key: fail, but let this key start a fresh sequence if it can
            FailSequence("Wrong key");
            buffer.Add(key);
            if (!IsPrefixOfAnyCombo(buffer)) buffer.Clear();
        }

        if (buffer.Count == 1) timer = sequenceTimeLimit;

        var completed = FindCompletedCombo();
        if (completed != null)
        {
            animator.SetTrigger(completed.animatorTrigger);
            if (sfxSourceGuitar != null && completed.guitarAtkSfx != null)
                PlayLimited(sfxSourceGuitar, completed.guitarAtkSfx);
            if (sfxSourceFlute != null && completed.fluteAtkSfx != null)
                PlayLimited(sfxSourceFlute, completed.fluteAtkSfx);
            if (sfxSourceDrums != null && completed.drumsAtkSfx != null)
                PlayLimited(sfxSourceDrums, completed.drumsAtkSfx);

            TriggerEffect(completed);
            buffer.Clear();
        }
    }

    // ---------------- Combo effects ----------------

    // Matches on the combo's name, so your existing combos in the Inspector keep working
    private void TriggerEffect(Combo combo)
    {
        switch (combo.name.Trim().ToLowerInvariant())
        {
            case "drums":
                StartCoroutine(DrumAoe());
                break;

            case "flute":
                if (playerMovement != null)
                    playerMovement.ApplySpeedBoost(speedBoostMultiplier, speedBoostDuration);
                else
                    Debug.LogWarning("ComboAttackController: no PlayerMovement found for the flute speed boost.");
                break;

            case "guitar":
                StartCoroutine(GuitarShoot());
                break;
        }
    }

    // DRUMS: damages every enemy within aoeRadius of the player
    private IEnumerator DrumAoe()
    {
        yield return new WaitForSeconds(aoeDelay);

        Vector3 center = transform.position;

        if (aoeEffectPrefab != null)
        {
            GameObject fx = Instantiate(aoeEffectPrefab, center, Quaternion.identity);
            Destroy(fx, 2f);
        }

        Collider[] hits = Physics.OverlapSphere(center, aoeRadius);
        var damaged = new HashSet<Enemy>(); // an enemy with several colliders is only hit once

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null && damaged.Add(enemy))
                enemy.TakeDamage(aoeDamage);
        }
    }

    // GUITAR: fires several projectiles that home in on the nearest enemies
    private IEnumerator GuitarShoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("ComboAttackController: Projectile Prefab is not assigned.");
            yield break;
        }

        yield return new WaitForSeconds(shotDelay);

        Vector3 origin = (firePoint != null ? firePoint : transform).position;
        List<Enemy> targets = GetEnemiesByDistance(origin);

        // base direction: toward the nearest enemy, or the way the player is facing
        Vector3 facing = new Vector3(Mathf.Sign(transform.localScale.x), 0f, 0f);
        Vector3 baseDir = facing;
        if (targets.Count > 0)
        {
            baseDir = targets[0].transform.position - origin;
            baseDir.y = 0f;
            if (baseDir.sqrMagnitude < 0.001f) baseDir = facing;
        }

        for (int i = 0; i < projectileCount; i++)
        {
            // projectile 0 -> nearest enemy, 1 -> second nearest, ... (wraps around if there are fewer enemies)
            Enemy target = targets.Count > 0 ? targets[i % targets.Count] : null;

            float angle = (i - (projectileCount - 1) * 0.5f) * spreadAngle;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir.normalized;

            HomingProjectile p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            p.Launch(target, projectileDamage, dir);

            yield return new WaitForSeconds(shotInterval);
        }
    }

    private List<Enemy> GetEnemiesByDistance(Vector3 origin)
    {
        var result = new List<Enemy>();
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(origin, e.transform.position) <= targetRange)
                result.Add(e);
        }

        result.Sort((a, b) =>
            (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));
        return result;
    }

    // Shows the drum AOE radius in the Scene view when the player is selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }

    // ---------------- Combo matching (unchanged) ----------------

    private bool IsPrefixOfAnyCombo(List<Key> seq)
    {
        foreach (var combo in combos)
        {
            if (seq.Count > combo.keys.Length) continue;
            bool match = true;
            for (int i = 0; i < seq.Count; i++)
                if (combo.keys[i] != seq[i]) { match = false; break; }
            if (match) return true;
        }
        return false;
    }

    private Combo FindCompletedCombo()
    {
        foreach (var combo in combos)
        {
            if (combo.keys.Length != buffer.Count) continue;
            bool match = true;
            for (int i = 0; i < buffer.Count; i++)
                if (combo.keys[i] != buffer[i]) { match = false; break; }
            if (match) return combo;
        }
        return null;
    }

    private void FailSequence(string reason)
    {
        Debug.Log($"Combo failed: {reason}");
        buffer.Clear();
    }

    // Metronome check: a key only counts if it is pressed on the beat.
    // Green = success, red = fail (the BeatBar flashes its rectangles).
    protected virtual bool IsInputAllowed()
    {
        if (beatBar == null) return true; // no beat bar assigned = no timing check (testing mode)

        bool onBeat = beatBar.IsOnBeat();
        beatBar.ShowResult(onBeat);
        return onBeat;
    }

    private void PlayLimited(AudioSource source, AudioClip clip)
    {
        if (sfxRoutines.TryGetValue(source, out var running) && running != null)
            StopCoroutine(running);

        source.Stop();
        source.volume = 1f;
        source.clip = clip;
        source.Play();

        sfxRoutines[source] = StartCoroutine(StopAfter(source, sfxDuration, sfxFadeOut));
    }

    private IEnumerator StopAfter(AudioSource source, float duration, float fadeTime)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duration - fadeTime));

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }

        source.Stop();
        source.volume = 1f;
    }
}