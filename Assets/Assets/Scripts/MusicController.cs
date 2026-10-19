using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Music / rhythm system.
// - Press the note keys (Z X C V) when the two markers cross the check zone ("on the beat").
// - On the beat  -> the note goes into the container, rectangles flash GREEN.
// - Off the beat -> the container is cleared, rectangles flash RED.
// Notes expire from the container after a while.
//When the container ends with the attack or ability sequence, that combo fires yayyyyy
public class MusicSystem : MonoBehaviour
{
    [Header("Note keys (note 1-4)")]
    [SerializeField] private Key[] noteKeys = { Key.Z, Key.X, Key.C, Key.V };

    [Header("Combos (use the letters of the keys above)")]
    [SerializeField] private string attackSequence = "ZCXV";
    [SerializeField] private string abilitySequence = "VXZC";

    [Header("Rhythm")]
    [SerializeField] private float bpm = 80f;               // beats per minute
    [SerializeField] private float hitWindow = 0.15f;       // seconds early/late that still counts
    [SerializeField] private float beatOffset = 0f;         // shift the beat to line up with your music
    [SerializeField] private float containerLifetime = 4f;  // a note is forgotten after this many seconds

    [Header("Beat bar UI")]
    [SerializeField] private RectTransform barTrack;     // the long bar (parent of the pieces below)
    [SerializeField] private RectTransform markerLeft;   // starts on the left, moves right
    [SerializeField] private RectTransform markerRight;  // starts on the right, moves left
    [SerializeField] private RectTransform checkZone;    // the middle rectangle
    [SerializeField] private Image[] feedbackImages;     // every rectangle that should flash green/red
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private float flashDuration = 0.25f;
    [SerializeField] private TMP_Text containerText;     // optional: shows the notes currently held

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip[] noteClips;      // one clip per note key, same order as Note Keys
    [SerializeField] private AudioClip missClip;

    // Other scripts (weapons, enemies dancing, UI) can subscribe to these
    public event System.Action<int, bool> OnNotePlayed;  // note index, success?
    public event System.Action OnAttack;
    public event System.Action OnAbility;

    private struct HeldNote
    {
        public int note;
        public float time;
    }

    private readonly List<HeldNote> container = new List<HeldNote>();
    private int[] attackNotes;
    private int[] abilityNotes;
    private Color[] baseColors;
    private Color flashColor;
    private float flashTimer;
    private float startTime;
    private PlayerHealth playerHealth;

    private float BeatInterval => 60f / Mathf.Max(bpm, 1f);

    private void Start()
    {
        attackNotes = ParseSequence(attackSequence);
        abilityNotes = ParseSequence(abilitySequence);

        baseColors = new Color[feedbackImages.Length];
        for (int i = 0; i < feedbackImages.Length; i++)
            baseColors[i] = feedbackImages[i].color;

        if (backgroundMusic != null) backgroundMusic.Play();
        startTime = Time.time;

        // stop the music game when the player dies
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnDied += HandlePlayerDied;

        RefreshContainerText();
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        enabled = false;
    }

    private void Update()
    {
        UpdateBar();
        UpdateFlash();
        ExpireOldNotes();

        if (Keyboard.current == null) return;
        for (int i = 0; i < noteKeys.Length; i++)
        {
            if (Keyboard.current[noteKeys[i]].wasPressedThisFrame)
                PressNote(i);
        }
    }

    // ---------------- Timing ----------------

    // 0..BeatInterval. The markers cross in the middle when this equals BeatInterval / 2.
    private float GetPhase()
    {
        float t = Time.time - startTime - beatOffset + BeatInterval * 0.5f;
        return Mathf.Repeat(t, BeatInterval);
    }

    private bool IsOnBeat()
    {
        return Mathf.Abs(GetPhase() - BeatInterval * 0.5f) <= hitWindow;
    }

    // ---------------- Notes and container stuff----------------

    private void PressNote(int note)
    {
        if (IsOnBeat())
        {
            container.Add(new HeldNote { note = note, time = Time.time });
            PlayNoteSound(note);
            Flash(successColor);
            OnNotePlayed?.Invoke(note, true);
            RefreshContainerText();
            CheckCombos();
        }
        else
        {
            // missed the bar -> container clears
            container.Clear();
            if (sfxSource != null && missClip != null) sfxSource.PlayOneShot(missClip);
            Flash(failColor);
            OnNotePlayed?.Invoke(note, false);
            RefreshContainerText();
        }
    }

    private void CheckCombos()
    {
        if (ContainerEndsWith(attackNotes))
        {
            Debug.Log("ATTACK!");
            container.Clear();
            RefreshContainerText();
            OnAttack?.Invoke();
        }
        else if (ContainerEndsWith(abilityNotes))
        {
            Debug.Log("SPECIAL ABILITY!");
            container.Clear();
            RefreshContainerText();
            OnAbility?.Invoke();
        }
    }

    private bool ContainerEndsWith(int[] sequence)
    {
        if (sequence.Length == 0 || container.Count < sequence.Length) return false;

        int offset = container.Count - sequence.Length;
        for (int i = 0; i < sequence.Length; i++)
        {
            if (container[offset + i].note != sequence[i]) return false;
        }
        return true;
    }

    private void ExpireOldNotes()
    {
        if (container.Count == 0) return;

        // notes are stored oldest-first, so just drop from the front
        bool changed = false;
        while (container.Count > 0 && Time.time - container[0].time > containerLifetime)
        {
            container.RemoveAt(0);
            changed = true;
        }
        if (changed) RefreshContainerText();
    }

    // "ZCXV" -> {0, 2, 1, 3} (index of each letter in Note Keys)
    private int[] ParseSequence(string sequence)
    {
        var result = new List<int>();
        foreach (char c in sequence.ToUpper())
        {
            int index = -1;
            for (int i = 0; i < noteKeys.Length; i++)
            {
                if (noteKeys[i].ToString() == c.ToString()) { index = i; break; }
            }

            if (index >= 0) result.Add(index);
            else Debug.LogWarning($"MusicSystem: '{c}' in \"{sequence}\" is not one of the Note Keys.");
        }
        return result.ToArray();
    }

    // ---------------- Visuals and audio ----------------

    private void UpdateBar()
    {
        if (barTrack == null || markerLeft == null || markerRight == null) return;

        float width = barTrack.rect.width;
        float t = GetPhase() / BeatInterval;          // 0..1, markers cross at 0.5
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);

        markerLeft.anchoredPosition = new Vector2(x, markerLeft.anchoredPosition.y);
        markerRight.anchoredPosition = new Vector2(-x, markerRight.anchoredPosition.y);

        // make the check zone exactly as wide as the hit window
        if (checkZone != null)
        {
            float pixelsPerSecond = width / BeatInterval;
            checkZone.sizeDelta = new Vector2(hitWindow * 2f * pixelsPerSecond, checkZone.sizeDelta.y);
        }
    }

    private void Flash(Color color)
    {
        flashColor = color;
        flashTimer = flashDuration;
    }

    private void UpdateFlash()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        float k = Mathf.Clamp01(flashTimer / flashDuration);
        for (int i = 0; i < feedbackImages.Length; i++)
            feedbackImages[i].color = Color.Lerp(baseColors[i], flashColor, k);
    }

    private void PlayNoteSound(int note)
    {
        if (sfxSource != null && noteClips != null && note < noteClips.Length && noteClips[note] != null)
            sfxSource.PlayOneShot(noteClips[note]);
    }

    private void RefreshContainerText()
    {
        if (containerText == null) return;

        string text = "";
        foreach (HeldNote held in container)
            text += noteKeys[held.note].ToString() + " ";
        containerText.text = text;
    }
}