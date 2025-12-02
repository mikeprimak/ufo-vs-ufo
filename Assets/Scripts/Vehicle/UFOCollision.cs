using UnityEngine;

/// <summary>
/// Handles Mario Kart-style collision bounces
/// UFO bounces off walls and gets briefly stunned
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class UFOCollision : MonoBehaviour
{
    [Header("Wall Collision Settings")]
    [Tooltip("How much force to bounce back from walls")]
    public float wallBounceForce = 20f;

    [Tooltip("Minimum impact speed to trigger wall bounce")]
    public float minWallImpactSpeed = 3f;

    [Header("Floor Collision Settings")]
    [Tooltip("Speed threshold for light vs heavy floor crash")]
    public float heavyCrashThreshold = 10f;

    [Tooltip("Bounce force for light floor touch")]
    public float lightFloorBounce = 2f;

    [Tooltip("Bounce force for heavy floor crash")]
    public float heavyFloorBounce = 8f;

    [Tooltip("How much horizontal momentum to keep when sliding on floor")]
    [Range(0f, 1f)]
    public float floorSlideRetention = 0.7f;

    [Tooltip("Minimum angle (from vertical) to be considered floor (degrees)")]
    public float floorAngleThreshold = 45f;

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
    private UFOCamera ufoCamera; // For triggering camera shake

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

        // Find UFOCamera in the scene (usually on Main Camera)
        ufoCamera = FindObjectOfType<UFOCamera>();
        if (ufoCamera == null)
        {
            Debug.LogWarning("UFOCollision: No UFOCamera found in scene. Camera shake will not work.");
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

        // Bounce state tracking (rotation now handled by UFOController.HandleAllRotation)
        if (isBouncing)
        {
            // Check if bounce is done (velocity near zero)
            if (rb.velocity.magnitude < 0.5f || Time.time >= bounceEndTime)
            {
                isBouncing = false;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Get collision info
        float impactSpeed = collision.relativeVelocity.magnitude;
        Vector3 collisionNormal = collision.contacts[0].normal;

        // Determine if this is floor or wall based on normal direction
        // Floor has normal pointing mostly upward (Y component close to 1)
        float normalAngleFromUp = Vector3.Angle(collisionNormal, Vector3.up);
        bool isFloor = normalAngleFromUp < floorAngleThreshold;

        if (isFloor)
        {
            HandleFloorCollision(collision, collisionNormal, impactSpeed);
        }
        else
        {
            HandleWallCollision(collision, collisionNormal, impactSpeed);
        }
    }

    void HandleWallCollision(Collision collision, Vector3 collisionNormal, float impactSpeed)
    {
        // Wall collision: use reflection bounce
        if (impactSpeed >= minWallImpactSpeed)
        {
            // Mark as bouncing (rotation handled by UFOController.HandleAllRotation)
            isBouncing = true;
            bounceEndTime = Time.time + 1f; // Max 1 second bounce duration

            // Get current velocity
            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = 0; // Keep horizontal only for flat bounces

            // Calculate reflection using physics: V' = V - 2(V·N)N
            // This creates natural deflection along walls at shallow angles
            Vector3 bounceDirection = Vector3.Reflect(currentVelocity.normalized, collisionNormal);

            // Stop current velocity
            rb.velocity = Vector3.zero;
            // Angular velocity cleared by HandleAllRotation every frame

            // Apply bounce force in reflected direction
            rb.AddForce(bounceDirection * wallBounceForce, ForceMode.VelocityChange);

            // Visual feedback - flash briefly
            StartFlash();

            // Camera shake based on impact speed
            if (ufoCamera != null)
            {
                ufoCamera.TriggerShakeFromImpact(impactSpeed);
            }
        }
    }

    void HandleFloorCollision(Collision collision, Vector3 collisionNormal, float impactSpeed)
    {
        // Get velocity components
        Vector3 currentVelocity = rb.velocity;
        float verticalSpeed = Mathf.Abs(currentVelocity.y); // How fast we're falling
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        float horizontalSpeed = horizontalVelocity.magnitude;

        // Calculate impact angle (0° = straight down, 90° = horizontal scrape)
        Vector3 impactDirection = -currentVelocity.normalized;
        float impactAngle = Vector3.Angle(impactDirection, Vector3.up);

        // Determine if this is a light touch or heavy crash
        bool isHeavyCrash = verticalSpeed >= heavyCrashThreshold;

        // Stop angular velocity
        rb.angularVelocity = Vector3.zero;

        // Disable vertical input temporarily to prevent fighting the bounce
        if (controller != null)
        {
            controller.DisableVerticalControl(0.3f);
        }

        // Angle-based behavior
        if (impactAngle < 30f)
        {
            // STEEP DESCENT (nearly straight down)
            // Check if there's any horizontal movement
            bool hasHorizontalMovement = horizontalSpeed > 0.5f;

            // Stop all velocity
            rb.velocity = Vector3.zero;

            if (isHeavyCrash && hasHorizontalMovement)
            {
                // Heavy crash with movement: bounce up with force
                rb.AddForce(Vector3.up * heavyFloorBounce, ForceMode.VelocityChange);
                StartFlash();

                // Mark as bouncing (rotation handled by UFOController.HandleAllRotation)
                isBouncing = true;
                bounceEndTime = Time.time + 0.5f;

                // Strong camera shake for heavy crash
                if (ufoCamera != null)
                {
                    ufoCamera.TriggerShake(1.0f); // Full intensity
                }
            }
            else if (hasHorizontalMovement)
            {
                // Light touch with movement: small bounce to prevent sticking
                rb.AddForce(Vector3.up * lightFloorBounce, ForceMode.VelocityChange);
            }
            // else: Pure vertical landing with no horizontal movement = no bounce (dead stop)
        }
        else if (impactAngle >= 30f && impactAngle < 60f)
        {
            // MEDIUM ANGLE (angled descent)
            // Bounce at an angle to keep flying away

            // Keep some horizontal momentum
            Vector3 slideDirection = horizontalVelocity.normalized;
            float retainedSpeed = horizontalSpeed * floorSlideRetention;

            // Add upward bounce
            float upwardBounce = isHeavyCrash ? heavyFloorBounce : lightFloorBounce;

            // Clear velocity and apply new bounce
            rb.velocity = Vector3.zero;
            rb.AddForce(slideDirection * retainedSpeed + Vector3.up * upwardBounce, ForceMode.VelocityChange);

            if (isHeavyCrash)
            {
                StartFlash();

                // Medium camera shake for angled heavy crash
                if (ufoCamera != null)
                {
                    ufoCamera.TriggerShake(0.7f); // Medium intensity
                }
            }
        }
        else
        {
            // SHALLOW ANGLE (scraping along floor)
            // Slide along floor with minimal vertical bounce

            // Keep most horizontal momentum
            Vector3 slideDirection = horizontalVelocity.normalized;
            float retainedSpeed = horizontalSpeed * 0.9f; // Keep 90% when scraping

            // Tiny upward bounce to prevent getting stuck
            rb.velocity = Vector3.zero;
            rb.AddForce(slideDirection * retainedSpeed + Vector3.up * lightFloorBounce, ForceMode.VelocityChange);

            // No flash for shallow scrapes
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
