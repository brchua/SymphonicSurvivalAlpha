using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float maxUpwardSpeed = 2f;

    private Rigidbody rb;
        private PlayerInput playerInput;
    private InputAction moveAction;
    private Camera mainCamera;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        moveAction = playerInput.actions.FindAction("Move", false);

        if (moveAction == null)
        {
            Debug.LogError(
                "PlayerMovement could not find an input action named 'Move'. " +
                "Check the PlayerInput actions asset and action name.",
                this);
        }
    }

    private void OnEnable()
    {
        moveInput = Vector2.zero;

        if (moveAction == null)
        {
            return;
        }

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        moveAction.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            moveAction.Disable();
        }

        moveInput = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 inputDirection = new Vector3(input.x, 0f, input.y);
        Vector3 worldDirection = inputDirection;

        if (mainCamera != null)
        {
            Quaternion cameraRotation = Quaternion.Euler(
                0f,
                mainCamera.transform.eulerAngles.y,
                0f);

            worldDirection = cameraRotation * inputDirection;
        }

        Vector3 velocity = rb.linearVelocity;
        velocity.x = worldDirection.x * moveSpeed;
        velocity.y = Mathf.Min(velocity.y, maxUpwardSpeed);
        velocity.z = worldDirection.z * moveSpeed;

        rb.linearVelocity = velocity;

        UpdateFacing(input.x);
    }

    private void UpdateFacing(float horizontalInput)
    {
        if (Mathf.Abs(horizontalInput) < 0.01f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Sign(horizontalInput) * Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }
}
