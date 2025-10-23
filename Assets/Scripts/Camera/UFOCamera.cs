using UnityEngine;

/// <summary>
/// Third-person camera that follows the UFO with tight rotation tracking
/// Wide FOV for arcade-style visibility similar to N64 games
/// Camera tracks UFO's physics rotation (not visual banking) for consistent aiming
/// Rotation Smoothing: 0.5-1.0 recommended for forward-firing weapons
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

    [Tooltip("How smoothly the camera follows position (lower = smoother, higher = tighter)")]
    public float smoothSpeed = 5f;

    [Header("Camera Rotation")]
    [Tooltip("How tightly camera tracks UFO rotation (higher = tighter, lower = smoother). 0.5-1.0 recommended for aiming.")]
    public float rotationSmoothing = 0.8f;

    [Tooltip("Tilt angle when looking at the UFO")]
    public float lookDownAngle = 10f;

    [Header("Vertical Movement Response")]
    [Tooltip("How much camera drops when UFO ascends (negative = drops down)")]
    public float verticalHeightOffset = -0.2f;

    [Tooltip("Camera pitch adjustment for vertical movement (degrees per unit of velocity). Positive = tilts up when ascending.")]
    public float verticalTiltAmount = 0.5f;

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
        // When ascending (positive velocity): tilt camera UP (negative pitch angle)
        // When descending (negative velocity): tilt camera DOWN (positive pitch angle)
        // Negate to invert: ascending should reduce the lookDownAngle
        float targetVerticalTilt = -verticalVelocity * verticalTiltAmount;
        currentVerticalTilt = Mathf.Lerp(currentVerticalTilt, targetVerticalTilt, verticalSmoothing * Time.deltaTime);

        // Calculate desired position behind and above the UFO
        // Camera rotates WITH the UFO's yaw for tighter turn tracking
        // Apply dynamic vertical offset
        Vector3 desiredPosition = target.position - (target.forward * distance) + (Vector3.up * (height + currentVerticalOffset));

        // Smoothly move camera to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Camera tracks UFO's physics rotation (not visual banking from UFO_Visual)
        // Extract only the Y rotation (yaw) from the target to keep horizon level
        Vector3 targetEuler = target.eulerAngles;
        Quaternion targetYawRotation = Quaternion.Euler(0, targetEuler.y, 0);

        // Calculate camera rotation: match UFO's yaw, add downward tilt + vertical movement tilt
        // This keeps the UFO visible while maintaining aiming direction
        Vector3 lookDirection = targetYawRotation * Vector3.forward;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection) * Quaternion.Euler(lookDownAngle + currentVerticalTilt, 0, 0);

        // Tighter rotation tracking for better aiming (higher value = more responsive)
        // Uses higher smoothing multiplier for near-instant rotation response
        float rotationSpeed = rotationSmoothing * 10f * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed);
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
