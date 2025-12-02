using UnityEngine;

/// <summary>
/// Simple arcade-style collision: bounce off surfaces and keep flying
/// On collision: reflect velocity, smoothly reorient to face new direction, continue
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class UFOCollision : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Bounce force multiplier")]
    public float bounceForce = 15f;

    [Tooltip("Minimum impact speed to trigger bounce")]
    public float minImpactSpeed = 2f;

    [Tooltip("Upward bias for floor bounces (keeps UFO airborne)")]
    public float floorUpwardBias = 10f;

    [Tooltip("How fast to reorient after collision (degrees per second)")]
    public float reorientSpeed = 360f;

    [Header("Visual Feedback")]
    [Tooltip("Flash the UFO on impact (optional)")]
    public Renderer ufoRenderer;

    [Tooltip("Color to flash on impact")]
    public Color flashColor = Color.red;

    [Tooltip("Flash duration in seconds")]
    public float flashDuration = 0.1f;

    private Rigidbody rb;
    private Color originalColor;
    private Material ufoMaterial;
    private float flashEndTime;
    private bool isFlashing;
    private UFOCamera ufoCamera;

    // Smooth reorientation
    private bool isReorienting;
    private float targetYaw;
    private float reorientEndTime;

    /// <summary>
    /// Returns true if UFO is currently reorienting after a collision.
    /// Used by UFOController to avoid fighting over rotation control.
    /// </summary>
    public bool IsReorienting => isReorienting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Get material for flashing
        if (ufoRenderer != null)
        {
            ufoMaterial = ufoRenderer.material;
            originalColor = ufoMaterial.color;
        }

        // Find UFOCamera for shake
        ufoCamera = FindObjectOfType<UFOCamera>();
    }

    void Update()
    {
        // Flash recovery
        if (isFlashing && Time.time >= flashEndTime)
        {
            isFlashing = false;
            if (ufoMaterial != null)
                ufoMaterial.color = originalColor;
        }

        // Smooth reorientation after collision
        if (isReorienting)
        {
            float currentYaw = transform.eulerAngles.y;

            // Calculate shortest rotation direction
            float yawDiff = Mathf.DeltaAngle(currentYaw, targetYaw);

            // Rotate toward target
            float maxRotation = reorientSpeed * Time.deltaTime;
            float newYaw;

            if (Mathf.Abs(yawDiff) <= maxRotation)
            {
                // Close enough - snap to target and stop
                newYaw = targetYaw;
                isReorienting = false;
            }
            else
            {
                // Rotate toward target
                newYaw = currentYaw + Mathf.Sign(yawDiff) * maxRotation;
            }

            // Apply rotation (keep current pitch, set roll to 0 for auto-level)
            float currentPitch = transform.eulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f;
            currentPitch = Mathf.Clamp(currentPitch, -80f, 80f); // Prevent gimbal lock

            transform.rotation = Quaternion.Euler(currentPitch, newYaw, 0f);

            // Timeout safety - don't reorient forever
            if (Time.time > reorientEndTime)
            {
                isReorienting = false;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed)
            return;

        Vector3 normal = collision.contacts[0].normal;

        // Determine if floor (normal pointing mostly up) or wall
        bool isFloor = Vector3.Dot(normal, Vector3.up) > 0.5f;

        // Calculate bounce direction
        Vector3 currentVel = rb.velocity;
        if (currentVel.magnitude < 0.1f)
        {
            // If nearly stopped, bounce directly away from surface
            currentVel = -normal * 5f;
        }

        Vector3 bounceDir;
        if (isFloor)
        {
            // Floor: reflect with strong upward bias to stay airborne
            bounceDir = Vector3.Reflect(currentVel.normalized, normal);
            bounceDir.y = Mathf.Max(bounceDir.y, 0.5f); // Ensure upward component
            bounceDir = (bounceDir.normalized + Vector3.up * 0.5f).normalized;
        }
        else
        {
            // Wall: simple reflection
            bounceDir = Vector3.Reflect(currentVel.normalized, normal);
        }

        // Apply bounce velocity
        float speed = Mathf.Max(impactSpeed * 0.7f, bounceForce);
        rb.velocity = bounceDir * speed;

        // Add extra upward force for floor hits
        if (isFloor)
        {
            rb.AddForce(Vector3.up * floorUpwardBias, ForceMode.VelocityChange);
        }

        // Start smooth reorientation to face bounce direction
        Vector3 flatBounceDir = new Vector3(bounceDir.x, 0, bounceDir.z);
        if (flatBounceDir.magnitude > 0.1f)
        {
            targetYaw = Mathf.Atan2(flatBounceDir.x, flatBounceDir.z) * Mathf.Rad2Deg;
            isReorienting = true;
            reorientEndTime = Time.time + 1f; // Max 1 second to reorient
        }

        // Visual feedback
        StartFlash();

        // Camera shake proportional to impact
        if (ufoCamera != null)
        {
            float shakeIntensity = Mathf.Clamp01(impactSpeed / 20f);
            ufoCamera.TriggerShake(shakeIntensity);
        }
    }

    void StartFlash()
    {
        if (ufoMaterial != null)
        {
            ufoMaterial.color = flashColor;
            isFlashing = true;
            flashEndTime = Time.time + flashDuration;
        }
    }
}
