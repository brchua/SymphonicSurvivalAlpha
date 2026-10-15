using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLock : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 23.1f;
    [SerializeField, Range(10f, 80f)] private float pitch = 30f;


    [SerializeField] private float smoothTime = 0.10f;
    [SerializeField] private float rotationSmoothing = 8f;

    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 12f;
    [SerializeField] private float zoomSmoothing = 10f;
    [SerializeField] private float[] rotAngles = new float[] { 45f, 135f, 225f, 315f };

    [SerializeField] private bool rotatePlayerWithCamera = true;

    private float targetZoom;
    private Camera cam;
    private Vector3 velocity;
    private Quaternion playerRotOffset = Quaternion.identity;

    private int rotationIndex;
    private float currentYaw;
    private float targetYaw;


    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetZoom = maxZoom;

        if (cam != null)
        {
            cam.orthographicSize = targetZoom;
        }

        rotationIndex = 0;
        currentYaw = rotAngles[rotationIndex];
        targetYaw = currentYaw;

        if (target != null)
        {
            Quaternion startView = Quaternion.Euler(pitch, currentYaw, 0f);
            playerRotOffset = Quaternion.Inverse(startView) * target.rotation;

            // start exactly in place instead of easing in from the scene position
            transform.position = target.position - startView * Vector3.forward * distance;
            transform.rotation = startView;
        }
    }

    private void OnValidate()
    {
        if (minZoom <= 0f)
        {
            minZoom = 0.1f;
        }

        if (maxZoom < minZoom)
        {
            maxZoom = minZoom + 1f;
        }

        smoothTime = Mathf.Max(0.0001f, smoothTime);
        zoomSmoothing = Mathf.Max(0.0001f, zoomSmoothing);
        rotationSmoothing = Mathf.Max(0.0001f, rotationSmoothing);

        if (rotAngles == null || rotAngles.Length == 0)
        {
            rotAngles = new float[] { 45f, 135f, 225f, 315f };
        }
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        if (target == null) return;

        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime));

        Quaternion viewRot = Quaternion.Euler(pitch, currentYaw, 0f);
        Vector3 desiredPosition = target.position - viewRot * Vector3.forward * distance;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.rotation = viewRot;

        if (rotatePlayerWithCamera)
        {
            target.rotation = transform.rotation * playerRotOffset;
        }

        if (cam != null)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime));
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            ToggleZoom();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleRot();
        }
    }

    public void ToggleZoom()
    {
        targetZoom = Mathf.Approximately(targetZoom, minZoom) ? maxZoom : minZoom;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    public void ToggleRot()
    {
        if (rotAngles == null || rotAngles.Length == 0) return;

        rotationIndex = (rotationIndex + 1) % rotAngles.Length;
        targetYaw = GetRoationAngle(rotationIndex);
    }

    public void OnToggleZoom(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleZoom();
        }
    }

    private float GetRoationAngle(int index)
    {
        if (rotAngles == null || rotAngles.Length == 0)
        {
            return 0f;
        }
        index = Mathf.Abs(index) % rotAngles.Length;
        return rotAngles[index];
    }
}
