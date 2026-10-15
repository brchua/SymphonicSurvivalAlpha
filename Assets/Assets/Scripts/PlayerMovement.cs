using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxUpwardSpeed = 2f;

    private Rigidbody rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        // Convert input to world-space using camera Y rotation so left/right match what you see on screen
        Vector3 desired;
        if (Camera.main != null)
        {
            // rotate input by camera Y only
            Quaternion camYaw = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            Vector3 input3 = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 worldMove = camYaw * input3;
            desired = new Vector3(worldMove.x * moveSpeed, rb.linearVelocity.y, worldMove.z * moveSpeed);

            // If player is a 2D sprite (quad), flipping its X scale makes it face left/right visually
            if (worldMove.sqrMagnitude > 0.001f)
            {
                Vector3 ls = transform.localScale;
                ls.x = Mathf.Sign(worldMove.x != 0f ? worldMove.x : Mathf.Sign(ls.x)) * Mathf.Abs(ls.x);
                transform.localScale = ls;
            }
        }
        else
        {
            // fallback to world axes
            desired = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
        }

        // keep upward speed clamped
        float clampedY = Mathf.Min(rb.linearVelocity.y, maxUpwardSpeed);
        rb.linearVelocity = new Vector3(desired.x, clampedY, desired.z);
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.name == "Ground") return;
        var contact = c.GetContact(0);
        Debug.Log($"HIT {c.gameObject.name}  pos.y={rb.position.y:F2}  vel.y={rb.linearVelocity.y:F02}  " +
                  $"impulse={c.impulse}  normal={contact.normal}  separation={contact.separation:F3}");
    }

    void OnCollisionStay(Collision c)
    {
        if (c.gameObject.name == "Ground") return;
        if (rb.linearVelocity.y > 2f)
            Debug.Log($"LAUNCH vs {c.gameObject.name}  vel.y={rb.linearVelocity.y:F2}  impulse={c.impulse}  sep={c.GetContact(0).separation:F3}");
    }
}
