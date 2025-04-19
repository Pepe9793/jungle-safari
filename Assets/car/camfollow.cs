using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public Transform target;                           // The object to follow
    public Vector3 offset = new Vector3(0f, 5f, -10f);  // Default offset
    public float followSpeed = 5f;
    public float rotationSpeed = 3f;

    private float yaw = 0f;
    private float pitch = 20f;

    void LateUpdate()
    {
        if (!target) return;

        // Mouse input for rotation
        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        pitch = Mathf.Clamp(pitch, -30f, 85f); // Limit vertical angle

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;

        // Calculate and apply desired position
        Vector3 desiredPosition = target.position + rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
    }
}
