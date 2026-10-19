using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FootStepSound : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float stepInterval = 0.4f;
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    private Rigidbody rb;
    private float stepTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Vector3 flatVelocity = rb.linearVelocity;
        flatVelocity.y = 0f;

        if (flatVelocity.magnitude < minSpeed)
        {
            stepTimer = 0f; // next step plays immediately when you start moving
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayStep();
            stepTimer = stepInterval;
        }
    }

    private void PlayStep()
    {
        if (footstepClips.Length == 0) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)]);
    }
}
