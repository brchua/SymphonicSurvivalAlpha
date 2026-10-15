using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxUpwardSpeed = 2f;

    private Rigidbody rb;
    private Vector2 moveInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // Update is called once per frame
    void Update()
    {
        float y = Mathf.Min(rb.linearVelocity.y, maxUpwardSpeed);
        rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
        //Debug.Log($"pos.y={rb.position.y:F2}  vel.y={rb.linearVelocity.y:F2}");
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.name == "Ground") return;
        var contact = c.GetContact(0);
        Debug.Log($"HIT {c.gameObject.name}  pos.y={rb.position.y:F2}  vel.y={rb.linearVelocity.y:F2}  " +
                  $"impulse={c.impulse}  normal={contact.normal}  separation={contact.separation:F3}");
    }

    void OnCollisionStay(Collision c)
    {
        if (c.gameObject.name == "Ground") return;
        if (rb.linearVelocity.y > 2f)
            Debug.Log($"LAUNCH vs {c.gameObject.name}  vel.y={rb.linearVelocity.y:F2}  impulse={c.impulse}  sep={c.GetContact(0).separation:F3}");
    }
}
