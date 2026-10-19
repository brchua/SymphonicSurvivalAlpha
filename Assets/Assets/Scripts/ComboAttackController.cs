using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ComboAttackController : MonoBehaviour
{
    [System.Serializable]
    public class Combo
    {
        public string name;
        public Key[] keys;
        public string animatorTrigger;
    }

    [SerializeField] private Animator animator;
    [SerializeField] private float sequenceTimeLimit = 5f;
    [SerializeField]
    private List<Combo> combos = new List<Combo>
    {
        new Combo { name = "Guitar", keys = new[] { Key.X, Key.C, Key.Z, Key.V }, animatorTrigger = "AttackGuitar" },
        new Combo { name = "Flute",  keys = new[] { Key.Z, Key.X, Key.C, Key.V }, animatorTrigger = "AttackFlute" },
    };

    private readonly List<Key> buffer = new List<Key>();
    private readonly HashSet<Key> comboKeys = new HashSet<Key>();
    private float timer;

    private void Awake()
    {
        RebuildKeySet();
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
            if (keyboard[key].wasPressedThisFrame && IsInputAllowed())
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
            buffer.Clear();
        }
    }

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

    // Metronome hook: later, return false when the press is off-beat
    protected virtual bool IsInputAllowed() => true;
}