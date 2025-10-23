using UnityEngine;

/// <summary>
/// Handles Mario Kart-style collision bounces
/// UFO bounces off walls and gets briefly stunned
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class UFOCollision : MonoBehaviour
{
    [Header("Collision Settings")]
    [Tooltip("How much force to bounce back on impact")]
    public float bounceForce = 4000f;

    [Tooltip("How long UFO is stunned after collision (seconds)")]
    public float stunDuration = 0.3f;

    [Tooltip("Minimum impact speed to trigger bounce")]
    public float minImpactSpeed = 3f;

    [Header("Visual Feedback")]
    [Tooltip("Flash the UFO on impact (optional)")]
    public Renderer ufoRenderer;

    [Tooltip("Color to flash on impact")]
    public Color flashColor = Color.red;

    private Rigidbody rb;
    private UFOController controller;
    private float stunEndTime;
    private bool isStunned;
    private Color originalColor;
    private Material ufoMaterial;
    private bool isBouncing;
    private float bounceEndTime;
    private Quaternion lockedRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<UFOController>();

        // Get material for flashing
        if (ufoRenderer != null)
        {
            ufoMaterial = ufoRenderer.material;
            originalColor = ufoMaterial.color;
        }
    }

    void Update()
    {
        // Flash effect recovery
        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;

            // Reset color
            if (ufoMaterial != null)
                ufoMaterial.color = originalColor;
        }

        // Lock rotation during bounce
        if (isBouncing)
        {
            // Force UFO to stay at locked rotation (can't turn during bounce)
            transform.rotation = lockedRotation;

            // Check if bounce is done (velocity near zero)
            if (rb.velocity.magnitude < 0.5f || Time.time >= bounceEndTime)
            {
                isBouncing = false;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit something hard enough
        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed >= minImpactSpeed)
        {
            // Lock current rotation (UFO will stay facing this direction during bounce)
            lockedRotation = transform.rotation;
            isBouncing = true;
            bounceEndTime = Time.time + 1f; // Max 1 second bounce duration

            // Get collision normal (perpendicular to the wall surface)
            Vector3 collisionNormal = collision.contacts[0].normal;

            // Get current velocity
            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = 0; // Keep horizontal only for flat bounces

            // Calculate reflection using physics: V' = V - 2(V·N)N
            // This creates natural deflection along walls at shallow angles
            Vector3 bounceDirection = Vector3.Reflect(currentVelocity.normalized, collisionNormal);

            // Stop current velocity
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero; // Stop any rotation

            // Apply bounce force in reflected direction
            rb.AddForce(bounceDirection * bounceForce, ForceMode.VelocityChange);

            // Visual feedback - flash briefly
            StartFlash();
        }
    }

    void StartFlash()
    {
        isStunned = true; // Reusing this as "isFlashing"
        stunEndTime = Time.time + 0.1f; // Flash for 100ms

        // Flash color
        if (ufoMaterial != null)
        {
            ufoMaterial.color = flashColor;
        }
    }

    public bool IsStunned()
    {
        return isStunned;
    }
}
