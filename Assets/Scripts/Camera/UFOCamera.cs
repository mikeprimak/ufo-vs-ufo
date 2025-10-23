using UnityEngine;

/// <summary>
/// Third-person camera that follows the UFO with smooth movement
/// Wide FOV for arcade-style visibility similar to N64 games
/// </summary>
public class UFOCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The UFO to follow")]
    public Transform target;

    [Header("Camera Position")]
    [Tooltip("Distance behind the UFO")]
    public float distance = 10f;

    [Tooltip("Height above the UFO")]
    public float height = 5f;

    [Tooltip("How smoothly the camera follows (lower = smoother)")]
    public float smoothSpeed = 5f;

    [Header("Camera Rotation")]
    [Tooltip("How smoothly the camera rotates to follow UFO direction")]
    public float rotationSmoothing = 3f;

    [Tooltip("Tilt angle when looking at the UFO")]
    public float lookDownAngle = 10f;

    [Header("Vertical Movement Response")]
    [Tooltip("How much camera drops when UFO ascends (negative = drops down)")]
    public float verticalHeightOffset = -0.2f;

    [Tooltip("Additional tilt up when ascending (positive = tilt up)")]
    public float verticalTiltAmount = 1.5f;

    [Tooltip("How smoothly vertical adjustments happen")]
    public float verticalSmoothing = 3f;

    [Header("Field of View")]
    [Tooltip("Wide FOV for arcade feel (N64-style)")]
    public float fieldOfView = 75f;

    private Camera cam;
    private Vector3 currentVelocity;
    private Rigidbody targetRigidbody;
    private float currentVerticalOffset;
    private float currentVerticalTilt;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }

        if (target == null)
        {
            Debug.LogWarning("UFOCamera: No target assigned! Please assign the UFO in the inspector.");
        }
        else
        {
            // Try to get the rigidbody for velocity tracking
            targetRigidbody = target.GetComponent<Rigidbody>();
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Get UFO's vertical velocity
        float verticalVelocity = 0f;
        if (targetRigidbody != null)
        {
            verticalVelocity = targetRigidbody.velocity.y;
        }

        // Calculate dynamic height offset based on vertical movement
        // When ascending (positive velocity): camera drops down (negative offset)
        // When descending (negative velocity): camera rises up (positive offset)
        float targetVerticalOffset = verticalVelocity * verticalHeightOffset;
        currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetVerticalOffset, verticalSmoothing * Time.deltaTime);

        // Calculate dynamic tilt based on vertical movement
        // When ascending: tilt up to see what's above
        // When descending: tilt down to see what's below
        float targetVerticalTilt = verticalVelocity * verticalTiltAmount;
        currentVerticalTilt = Mathf.Lerp(currentVerticalTilt, targetVerticalTilt, verticalSmoothing * Time.deltaTime);

        // Calculate desired position behind and above the UFO
        // Camera rotates WITH the UFO's yaw for tighter turn tracking
        // Apply dynamic vertical offset
        Vector3 desiredPosition = target.position - (target.forward * distance) + (Vector3.up * (height + currentVerticalOffset));

        // Smoothly move camera to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Camera rotation matches UFO's Y rotation (yaw) for tight turn tracking
        // Extract only the Y rotation from the target to avoid rolling with the UFO
        Vector3 targetEuler = target.eulerAngles;
        Quaternion targetYawRotation = Quaternion.Euler(0, targetEuler.y, 0);

        // Calculate look direction: rotate the "forward" direction by UFO's yaw, then tilt down slightly
        // Apply dynamic vertical tilt based on ascending/descending
        Vector3 lookDirection = targetYawRotation * Vector3.forward;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection) * Quaternion.Euler(lookDownAngle + currentVerticalTilt, 0, 0);

        // Smoothly rotate camera to match UFO's yaw and vertical movement
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
    }

    // Helper method to adjust camera settings at runtime
    public void SetCameraDistance(float newDistance)
    {
        distance = newDistance;
    }

    public void SetCameraHeight(float newHeight)
    {
        height = newHeight;
    }
}
