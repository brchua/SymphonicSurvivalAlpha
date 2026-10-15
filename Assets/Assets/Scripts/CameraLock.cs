using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLock : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-5.5f, 14f, -10f);
    [SerializeField] private float smoothTime = 0.10f;

    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 12f;
    [SerializeField] private float zoomSmoothing = 10f;

    private float targetZoom;
    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = targetZoom;
        targetZoom = maxZoom;
    }


    // Update is called once per frame
    void Update()
    {
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSmoothing * Time.deltaTime);

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            ToggleZoom();
        }
    }

    public void ToggleZoom()
    {
        targetZoom = Mathf.Approximately(targetZoom, minZoom) ? maxZoom : minZoom;
    }
}
