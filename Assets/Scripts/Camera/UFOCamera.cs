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
    public float distance = 25f;

    [Tooltip("Height above the UFO")]
    public float height = 8f;

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

    [Tooltip("How quickly FOV kicks in/out")]
    public float fovKickSpeed = 5f;

    [Header("Dynamic Zoom Out (Game Feel)")]
    [Tooltip("Enable dynamic camera zoom out during sharp turns")]
    public bool enableTurnZoomOut = true;

    [Tooltip("Additional distance when making sharp turns (added to base distance)")]
    public float turnZoomOutDistance = 3f;

    [Tooltip("Angular velocity threshold to trigger zoom out (degrees/sec)")]
    public float turnZoomThreshold = 90f;

    [Tooltip("How quickly camera zooms in/out during turns")]
    public float turnZoomSpeed = 4f;

    [Header("Camera Shake (Game Feel)")]
    [Tooltip("Enable camera shake on impacts")]
    public bool enableCameraShake = false;

    [Tooltip("How long shake lasts (seconds)")]
    public float shakeDuration = 0.3f;

    [Tooltip("Maximum shake intensity (position offset in units)")]
    public float shakeIntensity = 0.4f;

    [Tooltip("How quickly shake decays")]
    public float shakeDecaySpeed = 5f;

    [Tooltip("Minimum impact speed to trigger shake (units/s)")]
    public float minShakeSpeed = 15f;

    [Tooltip("Minimum time between shakes (seconds)")]
    public float shakeCooldown = 0.3f;

    [Header("Death Camera Settings")]
    [Tooltip("Enable dramatic camera zoom out when player UFO dies")]
    public bool enableDeathZoom = true;

    [Tooltip("Distance behind UFO when dead")]
    public float deathCameraDistance = 15f;

    [Tooltip("Height above UFO when dead (bird's eye view)")]
    public float deathCameraHeight = 25f;

    [Tooltip("How quickly camera moves to death position")]
    public float deathCameraSpeed = 3f;

    [Header("Camera Collision Settings")]
    [Tooltip("Enable camera collision detection to prevent clipping through walls")]
    public bool enableCameraCollision = false;  // Disabled to test vibration

    [Tooltip("Radius of camera collision sphere (larger = more padding from walls)")]
    public float cameraCollisionRadius = 0.5f;

    [Tooltip("Layers to check for camera collision (usually walls/environment)")]
    public LayerMask collisionLayers = -1; // Default: all layers

    [Tooltip("How quickly camera pulls in when obstructed")]
    public float collisionPullInSpeed = 10f;

    [Tooltip("How quickly camera returns to desired distance when clear")]
    public float collisionRecoverySpeed = 5f;

    private Camera cam;
    private Vector3 currentVelocity;
    private Rigidbody targetRigidbody;
    private float currentVerticalOffset;
    private float currentVerticalTilt;
    private float currentDistance;
    private float currentFOV;
    private UFOController ufoController; // For detecting acceleration/braking input
    private float currentTurnZoomOut; // Turn-based distance offset
    private Quaternion lastTargetRotation; // For measuring rotation delta

    // Camera shake state
    private float shakeTimeRemaining;
    private float currentShakeIntensity;
    private Vector3 shakeOffset;
    private float lastShakeTime;

    // Death camera state
    private bool isTargetDead = false;
    private UFOHealth targetHealth;
    private Vector3 deathCameraTargetPosition; // Fixed position for death camera
    private Quaternion lastAliveRotation; // Rotation when UFO died (for camera angle)

    // Camera collision state
    private float adjustedDistance; // Distance after collision adjustment

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
        adjustedDistance = distance;

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
            // Try to get UFOHealth for death detection
            targetHealth = target.GetComponent<UFOHealth>();

            // IMMEDIATELY position camera behind UFO (no lerp delay)
            // True 3D flight: use UFO's local axes
            Vector3 initialPosition = target.position - (target.forward * distance) + (target.up * height);
            transform.position = initialPosition;

            // IMMEDIATELY set camera rotation to match UFO's aim direction exactly
            // No tilt - screen center = aim point
            transform.rotation = target.rotation;

            // Initialize last rotation for turn detection
            lastTargetRotation = target.rotation;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Check if target UFO died this frame
        if (enableDeathZoom && targetHealth != null && targetHealth.IsDead() && !isTargetDead)
        {
            isTargetDead = true;
            lastAliveRotation = target.rotation; // Remember which way UFO was facing when it died
        }

        // Get UFO's vertical velocity and forward speed
        float verticalVelocity = 0f;
        float forwardSpeed = 0f;
        if (targetRigidbody != null)
        {
            verticalVelocity = targetRigidbody.velocity.y;
            // Calculate forward speed relative to UFO's facing direction
            forwardSpeed = Vector3.Dot(targetRigidbody.velocity, target.forward);
        }

        // === TURN ZOOM OUT SYSTEM (Game Feel) ===
        // Detect sharp turns by measuring rotation delta (since UFO uses MoveRotation, not physics rotation)
        float targetTurnZoomOut = 0f;
        if (enableTurnZoomOut && target != null)
        {
            // Calculate rotation delta since last frame
            Quaternion rotationDelta = target.rotation * Quaternion.Inverse(lastTargetRotation);

            // Convert to angle in degrees
            rotationDelta.ToAngleAxis(out float angleDelta, out Vector3 axis);

            // Calculate angular speed (degrees per second)
            float angularSpeed = angleDelta / Time.deltaTime;

            // If turning faster than threshold, zoom out proportionally
            if (angularSpeed > turnZoomThreshold)
            {
                // Normalize turn speed (0-1 range, capped at 2x threshold)
                float turnIntensity = Mathf.Clamp01((angularSpeed - turnZoomThreshold) / turnZoomThreshold);
                targetTurnZoomOut = turnIntensity * turnZoomOutDistance;
            }

            // Store current rotation for next frame
            lastTargetRotation = target.rotation;
        }

        // Smoothly lerp current turn zoom out
        currentTurnZoomOut = Mathf.Lerp(currentTurnZoomOut, targetTurnZoomOut, turnZoomSpeed * Time.deltaTime);

        // === DEATH CAMERA MODE (completely different behavior) ===
        if (isTargetDead && enableDeathZoom)
        {
            // Death camera: fixed elevated position behind UFO, looking down at the falling wreck
            // Calculate death camera position: behind and high above UFO
            Vector3 deathDirection = lastAliveRotation * -Vector3.forward; // Behind where UFO was facing when it died
            Vector3 targetDeathPosition = target.position + (deathDirection * deathCameraDistance) + (Vector3.up * deathCameraHeight);

            // Smoothly move to death camera position
            transform.position = Vector3.Lerp(transform.position, targetDeathPosition, deathCameraSpeed * Time.deltaTime);

            // Always look directly at the UFO (not following its rotation)
            Vector3 lookAtTarget = target.position + Vector3.up * 2f; // Look at center of UFO, slightly above
            Quaternion deathCameraRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, deathCameraRotation, deathCameraSpeed * Time.deltaTime);

            // Early return - skip all normal camera logic
            return;
        }

        // === NORMAL CAMERA MODE (alive UFO) ===
        // Determine if UFO is reversing and adjust camera accordingly
        bool isReversing = forwardSpeed < reverseSpeedThreshold;
        float baseDistance = isReversing ? reverseDistance : distance;
        float targetDistance = baseDistance + currentTurnZoomOut; // Add turn zoom out
        float baseFOV = isReversing ? reverseFOV : fieldOfView;

        // === FOV KICK SYSTEM (Game Feel) ===
        float fovModifier = 0f;
        if (enableFOVKick)
        {
            // Check if accelerating or braking
            bool isAccelerating = Input.GetKey(KeyCode.A) || Input.GetButton("Fire1");
            bool isBraking = Input.GetKey(KeyCode.D) || Input.GetButton("Fire2");

            // Apply FOV modifiers based on input
            if (isAccelerating && !isBraking)
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
        Vector3 cameraHorizontalVel = new Vector3(targetRigidbody.velocity.x, 0, targetRigidbody.velocity.z);
        float cameraHorizontalSpeed = cameraHorizontalVel.magnitude;

        // Calculate dynamic height offset based on vertical movement
        // Dramatic effect ONLY for forward+ascend, subtle for everything else
        float targetVerticalOffset = 0f;
        if (verticalVelocity > 0 && cameraHorizontalSpeed > 5f) // Ascending WITH forward movement
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
        if (verticalVelocity > 0 && cameraHorizontalSpeed > 5f) // Ascending WITH forward movement
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

        // === CAMERA COLLISION SYSTEM ===
        // Raycast from target to desired camera position to check for obstructions
        // True 3D flight: camera follows UFO's local axes (behind and above in UFO space)
        // No vertical offset bobbing - stable camera position for fixed aim point
        Vector3 targetToCamera = -target.forward * currentDistance;
        Vector3 desiredCameraOffset = targetToCamera + (target.up * height);
        Vector3 desiredPosition = target.position + desiredCameraOffset;

        // Check for obstructions between target and desired camera position
        if (enableCameraCollision)
        {
            Vector3 rayDirection = desiredCameraOffset.normalized;
            float desiredDistanceFromTarget = desiredCameraOffset.magnitude;

            // Raycast with sphere to detect walls/obstacles
            RaycastHit hit;
            if (Physics.SphereCast(target.position, cameraCollisionRadius, rayDirection, out hit, desiredDistanceFromTarget, collisionLayers))
            {
                // Camera is obstructed - pull it closer to avoid clipping through walls
                float safeDistance = hit.distance - cameraCollisionRadius; // Add padding
                safeDistance = Mathf.Max(safeDistance, 1f); // Minimum distance of 1 unit

                // Smoothly pull camera in when obstructed (fast)
                adjustedDistance = Mathf.Lerp(adjustedDistance, safeDistance, collisionPullInSpeed * Time.deltaTime);

                // Recalculate position with adjusted distance
                Vector3 adjustedOffset = rayDirection * adjustedDistance;
                desiredPosition = target.position + adjustedOffset;

                // Debug visualization (uncomment to see in Scene view)
                #if UNITY_EDITOR
                Debug.DrawLine(target.position, hit.point, Color.red);
                Debug.DrawLine(hit.point, desiredPosition, Color.yellow);
                #endif
            }
            else
            {
                // No obstruction - smoothly return to desired distance (slower)
                adjustedDistance = Mathf.Lerp(adjustedDistance, desiredDistanceFromTarget, collisionRecoverySpeed * Time.deltaTime);

                // Use adjusted distance for smooth recovery
                Vector3 adjustedOffset = rayDirection * adjustedDistance;
                desiredPosition = target.position + adjustedOffset;

                // Debug visualization (uncomment to see in Scene view)
                #if UNITY_EDITOR
                Debug.DrawLine(target.position, desiredPosition, Color.green);
                #endif
            }
        }

        // INSTANT camera position - no smoothing, locked to UFO for stable aim
        transform.position = desiredPosition;

        // Apply shake DIRECTLY to final position
        transform.position += shakeOffset;

        // Camera rotation (normal mode only - death mode handles rotation above)
        // True 3D flight: camera looks exactly where UFO is aiming
        // INSTANT rotation - no smoothing, aim point locked to screen center

        // Match UFO's rotation exactly (no lerp/slerp - instant response)
        transform.rotation = target.rotation;
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
        // DISABLED - all shake disabled to eliminate vibration
        return;

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
        // DISABLED - all shake disabled to eliminate vibration
        return;

        if (!enableCameraShake)
            return;

        // Check cooldown - don't shake if too soon after last shake
        float timeSinceLastShake = Time.time - lastShakeTime;
        if (timeSinceLastShake < shakeCooldown)
            return;

        // Check minimum speed threshold - ignore weak impacts
        if (impactSpeed < minShakeSpeed)
            return;

        // Normalize impact speed to 0-1 range
        float intensity = Mathf.Clamp01(impactSpeed / maxSpeed);

        // Update last shake time
        lastShakeTime = Time.time;

        TriggerShake(intensity);
    }
}
