using UnityEngine;

public class CameraLock : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-10f, 10f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity;

    // Update is called once per frame
    void Update()
    {
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
