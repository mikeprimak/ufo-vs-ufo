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
    public float verticalHeightOffset = -10f;

    [Tooltip("Camera pitch adjustment for vertical movement (degrees per unit of velocity). Positive = tilts up when ascending.")]
    public float verticalTiltAmount = 1.2f;

    [Tooltip("How smoothly vertical adjustments happen")]
    public float verticalSmoothing = 3f;

    [Header("Field of View")]
    [Tooltip("Wide FOV for arcade feel (N64-style)")]
    public float fieldOfView = 75f;

    [Header("Reverse Camera Settings")]
    [Tooltip("Camera distance when reversing (larger = pulls back further)")]
    public float reverseDistance = 15f;

    [Tooltip("FOV when reversing (wider for better visibility)")]
    public float reverseFOV = 90f;

    [Tooltip("Forward speed threshold to trigger reverse camera (negative = reversing)")]
    public float reverseSpeedThreshold = -1f;

    [Tooltip("How smoothly reverse camera transitions happen")]
    public float reverseCameraSmoothing = 3f;

    [Header("FOV Kick (Game Feel)")]
    [Tooltip("Enable FOV kick on acceleration/braking for speed rush feel")]
    public bool enableFOVKick = true;

    [Tooltip("FOV increase when accelerating (e.g., 5 = 75 FOV becomes 80)")]
    public float accelerationFOVBoost = 5f;

    [Tooltip("FOV decrease when braking hard (e.g., 5 = 75 FOV becomes 70)")]
    public float brakeFOVReduction = 5f;

    [Tooltip("FOV increase during combo boost (e.g., 10 = 75 FOV becomes 85)")]
    public float comboBoostFOVBoost = 10f;

    [Tooltip("How quickly FOV kicks in/out")]
    public float fovKickSpeed = 5f;

    [Header("Camera Shake (Game Feel)")]
    [Tooltip("Enable camera shake on impacts")]
    public bool enableCameraShake = true;

    [Tooltip("How long shake lasts (seconds)")]
    public float shakeDuration = 0.2f;

    [Tooltip("Maximum shake intensity (position offset in units)")]
    public float shakeIntensity = 0.5f;

    [Tooltip("How quickly shake decays")]
    public float shakeDecaySpeed = 3f;

    private Camera cam;
    private Vector3 currentVelocity;
    private Rigidbody targetRigidbody;
    private float currentVerticalOffset;
    private float currentVerticalTilt;
    private float currentDistance;
    private float currentFOV;
    private UFOController ufoController; // For detecting acceleration/braking input

    // Camera shake state
    private float shakeTimeRemaining;
    private float currentShakeIntensity;
    private Vector3 shakeOffset;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
            currentFOV = fieldOfView;
        }

        // Initialize current distance to normal distance
        currentDistance = distance;

        if (target == null)
        {
            Debug.LogWarning("UFOCamera: No target assigned! Please assign the UFO in the inspector.");
        }
        else
        {
            // Try to get the rigidbody for velocity tracking
            targetRigidbody = target.GetComponent<Rigidbody>();
            // Try to get UFOController for input detection
            ufoController = target.GetComponent<UFOController>();
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Get UFO's vertical velocity and forward speed
        float verticalVelocity = 0f;
        float forwardSpeed = 0f;
        if (targetRigidbody != null)
        {
            verticalVelocity = targetRigidbody.velocity.y;
            // Calculate forward speed relative to UFO's facing direction
            forwardSpeed = Vector3.Dot(targetRigidbody.velocity, target.forward);
        }

        // Determine if UFO is reversing and adjust camera accordingly
        bool isReversing = forwardSpeed < reverseSpeedThreshold;
        float targetDistance = isReversing ? reverseDistance : distance;
        float baseFOV = isReversing ? reverseFOV : fieldOfView;

        // === FOV KICK SYSTEM (Game Feel) ===
        float fovModifier = 0f;
        if (enableFOVKick)
        {
            // Check if accelerating or braking
            bool isAccelerating = Input.GetKey(KeyCode.A) || Input.GetButton("Fire1");
            bool isBraking = Input.GetKey(KeyCode.D) || Input.GetButton("Fire2");

            // Check if combo boost is active (if UFOController exists)
            bool isComboBoostActive = false;
            if (ufoController != null)
            {
                // Access combo boost state from UFOController
                isComboBoostActive = ufoController.IsComboBoostActive();
            }

            // Apply FOV modifiers based on input
            if (isComboBoostActive)
            {
                fovModifier = comboBoostFOVBoost; // Biggest kick for combo boost
            }
            else if (isAccelerating && !isBraking)
            {
                fovModifier = accelerationFOVBoost; // Speed rush feel
            }
            else if (isBraking && !isAccelerating)
            {
                fovModifier = -brakeFOVReduction; // Zoom in slightly when braking
            }
        }

        float targetFOV = baseFOV + fovModifier;

        // Smoothly transition camera distance and FOV
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, reverseCameraSmoothing * Time.deltaTime);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovKickSpeed * Time.deltaTime);

        // Apply FOV to camera
        if (cam != null)
        {
            cam.fieldOfView = currentFOV;
        }

        // Calculate horizontal speed to detect pure vertical vs angled movement
        Vector3 horizontalVelocity = new Vector3(targetRigidbody.velocity.x, 0, targetRigidbody.velocity.z);
        float horizontalSpeed = horizontalVelocity.magnitude;

        // Calculate dynamic height offset based on vertical movement
        // Dramatic effect ONLY for forward+ascend, subtle for everything else
        float targetVerticalOffset = 0f;
        if (verticalVelocity > 0 && horizontalSpeed > 5f) // Ascending WITH forward movement
        {
            // Camera drops down dramatically when ascending while moving forward
            targetVerticalOffset = verticalVelocity * verticalHeightOffset;
        }
        else // Pure vertical or descending - use original subtle behavior
        {
            // Camera barely moves (original subtle behavior)
            targetVerticalOffset = verticalVelocity * (verticalHeightOffset * 0.2f);
        }
        currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetVerticalOffset, verticalSmoothing * Time.deltaTime);

        // Calculate dynamic tilt based on vertical movement
        // Dramatic effect ONLY for forward+ascend, subtle for everything else
        float targetVerticalTilt = 0f;
        if (verticalVelocity > 0 && horizontalSpeed > 5f) // Ascending WITH forward movement
        {
            // Camera tilts up more when ascending while moving forward
            targetVerticalTilt = -verticalVelocity * verticalTiltAmount;
        }
        else // Pure vertical or descending - use original subtle behavior
        {
            // Camera barely tilts (original subtle behavior)
            targetVerticalTilt = -verticalVelocity * (verticalTiltAmount * 0.5f);
        }
        currentVerticalTilt = Mathf.Lerp(currentVerticalTilt, targetVerticalTilt, verticalSmoothing * Time.deltaTime);

        // === CAMERA SHAKE SYSTEM (Game Feel) ===
        if (enableCameraShake && shakeTimeRemaining > 0)
        {
            // Decay shake over time
            shakeTimeRemaining -= Time.deltaTime;
            currentShakeIntensity = Mathf.Lerp(currentShakeIntensity, 0f, shakeDecaySpeed * Time.deltaTime);

            // Generate random shake offset
            shakeOffset = Random.insideUnitSphere * currentShakeIntensity;

            // Clamp shake when nearly done
            if (shakeTimeRemaining <= 0)
            {
                shakeOffset = Vector3.zero;
                currentShakeIntensity = 0f;
            }
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // Calculate desired position behind and above the UFO
        // Camera rotates WITH the UFO's yaw for tighter turn tracking
        // Apply dynamic vertical offset and current distance (which adjusts for reverse)
        Vector3 desiredPosition = target.position - (target.forward * currentDistance) + (Vector3.up * (height + currentVerticalOffset));

        // Add shake offset to desired position
        desiredPosition += shakeOffset;

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

    /// <summary>
    /// Trigger camera shake with custom intensity
    /// Call this from collision scripts for impact feedback
    /// </summary>
    /// <param name="intensity">Shake strength (0-1), 1.0 = full shakeIntensity</param>
    public void TriggerShake(float intensity = 1.0f)
    {
        if (!enableCameraShake)
            return;

        shakeTimeRemaining = shakeDuration;
        currentShakeIntensity = shakeIntensity * Mathf.Clamp01(intensity);
    }

    /// <summary>
    /// Trigger camera shake based on impact speed
    /// Automatically scales intensity based on how hard you hit
    /// </summary>
    /// <param name="impactSpeed">Speed of impact (will be normalized)</param>
    /// <param name="maxSpeed">Maximum expected impact speed for normalization</param>
    public void TriggerShakeFromImpact(float impactSpeed, float maxSpeed = 30f)
    {
        if (!enableCameraShake)
            return;

        // Normalize impact speed to 0-1 range
        float intensity = Mathf.Clamp01(impactSpeed / maxSpeed);

        // Only shake if impact is significant (>= 10% of max speed)
        // Changed from > to >= to match minWallImpactSpeed threshold (3.0)
        if (intensity >= 0.1f)
        {
            Debug.Log($"[Camera Shake] Impact: {impactSpeed:F1} units/s, Intensity: {intensity:F2}, Shake Amount: {shakeIntensity * intensity:F3} units");
            TriggerShake(intensity);
        }
        else
        {
            Debug.Log($"[Camera Shake] Skipped (too weak). Impact: {impactSpeed:F1}, Need >= {maxSpeed * 0.1f:F1}");
        }
    }
}
