using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// NecroDancer-style beat bar (the metronome check).
// Markers slide in from both sides of the bar and reach the middle rectangle on every beat.
// ComboAttackController asks IsOnBeat() when a note key is pressed.
public class BeatBar : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private float bpm = 80f;          // beats per minute
    [SerializeField] private float hitWindow = 0.15f;  // seconds early/late that still counts (keep under half a beat)
    [SerializeField] private float beatOffset = 0f;    // shift the beat to line up with your music
    [SerializeField] private int travelBeats = 3;      // how many beats ahead you can see markers coming

    [Header("Beat bar UI")]
    [SerializeField] private RectTransform barTrack;        // the long bar
    [SerializeField] private RectTransform markerTemplate;  // ONE marker; the script makes the copies it needs
    [SerializeField] private RectTransform checkZone;       // the big rectangle in the middle
    [SerializeField] private Image[] feedbackImages;        // every rectangle that should flash green/red
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private float flashDuration = 0.25f;

    [Header("Music (optional)")]
    [SerializeField] private AudioSource backgroundMusic;   // if set, the beat follows this song's time

    // Fires on every beat (use it later for enemy dancing, pulsing UI, etc.)
    public event System.Action OnBeat;

    private readonly List<RectTransform> leftMarkers = new List<RectTransform>();
    private readonly List<RectTransform> rightMarkers = new List<RectTransform>();
    private Color[] baseColors;
    private Color flashColor;
    private float flashTimer;
    private float startTime;
    private int lastBeat;

    private float Interval => 60f / Mathf.Max(bpm, 1f);

    private void Start()
    {
        baseColors = new Color[feedbackImages.Length];
        for (int i = 0; i < feedbackImages.Length; i++)
            baseColors[i] = feedbackImages[i].color;

        BuildMarkerPool();

        if (backgroundMusic != null) backgroundMusic.Play();
        startTime = Time.time;
        lastBeat = Mathf.FloorToInt(GetBeatTime() / Interval);
    }

    private void Update()
    {
        UpdateBeatEvent();
        UpdateMarkers();
        UpdateFlash();
    }

    // ---------------- Public API ----------------

    // True if "now" is within the hit window of a beat
    public bool IsOnBeat()
    {
        float t = GetBeatTime();
        float nearestBeat = Mathf.Round(t / Interval) * Interval;
        return Mathf.Abs(t - nearestBeat) <= hitWindow;
    }

    // Flashes every feedback rectangle green (success) or red (fail)
    public void ShowResult(bool success)
    {
        flashColor = success ? successColor : failColor;
        flashTimer = flashDuration;
    }

    // ---------------- Timing ----------------

    private float GetBeatTime()
    {
        float songTime = (backgroundMusic != null && backgroundMusic.isPlaying)
            ? backgroundMusic.time
            : Time.time - startTime;
        return songTime - beatOffset;
    }

    private void UpdateBeatEvent()
    {
        int beat = Mathf.FloorToInt(GetBeatTime() / Interval);
        if (beat > lastBeat)
        {
            lastBeat = beat;
            OnBeat?.Invoke();
        }
    }

    // ---------------- Visuals ----------------

    private void BuildMarkerPool()
    {
        if (barTrack == null || markerTemplate == null)
        {
            Debug.LogError("BeatBar: Bar Track and Marker Template must be assigned.");
            return;
        }

        int pairs = Mathf.Max(1, travelBeats) + 2; // enough for every beat that can be on screen
        for (int i = 0; i < pairs; i++)
        {
            leftMarkers.Add(Instantiate(markerTemplate, barTrack));
            rightMarkers.Add(Instantiate(markerTemplate, barTrack));
        }
        markerTemplate.gameObject.SetActive(false); // hide the original, only the copies are used
    }

    private void UpdateMarkers()
    {
        if (leftMarkers.Count == 0) return;

        float t = GetBeatTime();
        float travelTime = Mathf.Max(1, travelBeats) * Interval;
        float halfWidth = barTrack.rect.width * 0.5f;
        int nextBeat = Mathf.CeilToInt(t / Interval); // the first beat that hasn't happened yet

        for (int i = 0; i < leftMarkers.Count; i++)
        {
            float timeUntil = (nextBeat + i) * Interval - t; // seconds until this beat
            bool visible = timeUntil <= travelTime;

            SetActive(leftMarkers[i], visible);
            SetActive(rightMarkers[i], visible);
            if (!visible) continue;

            // far away = at the edge, on the beat = in the middle
            float x = (timeUntil / travelTime) * halfWidth;
            leftMarkers[i].anchoredPosition = new Vector2(-x, leftMarkers[i].anchoredPosition.y);
            rightMarkers[i].anchoredPosition = new Vector2(x, rightMarkers[i].anchoredPosition.y);
        }

        // make the middle rectangle exactly as wide as the hit window
        if (checkZone != null)
        {
            float pixelsPerSecond = halfWidth / travelTime;
            checkZone.sizeDelta = new Vector2(hitWindow * 2f * pixelsPerSecond, checkZone.sizeDelta.y);
        }
    }

    private static void SetActive(RectTransform rect, bool active)
    {
        if (rect.gameObject.activeSelf != active)
            rect.gameObject.SetActive(active);
    }

    private void UpdateFlash()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        float k = Mathf.Clamp01(flashTimer / flashDuration);
        for (int i = 0; i < feedbackImages.Length; i++)
            feedbackImages[i].color = Color.Lerp(baseColors[i], flashColor, k);
    }
}