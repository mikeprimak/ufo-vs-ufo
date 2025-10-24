using UnityEngine;

/// <summary>
/// Homing missile that tracks and follows the nearest target
/// Extends basic projectile with seeking behavior
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HomingProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Initial speed of the missile")]
    public float speed = 40f;

    [Tooltip("How long before missile destroys itself (seconds)")]
    public float lifetime = 8f;

    [Tooltip("Damage dealt on hit (reserved for future health system)")]
    public int damage = 20;

    [Header("Homing Settings")]
    [Tooltip("How quickly missile turns toward target (degrees per second)")]
    public float turnRate = 180f;

    [Tooltip("How quickly missile accelerates toward max speed")]
    public float acceleration = 20f;

    [Tooltip("Maximum speed the missile can reach")]
    public float maxSpeed = 60f;

    [Tooltip("Detection radius to find targets")]
    public float detectionRadius = 100f;

    [Tooltip("Tag to search for targets (default: Player)")]
    public string targetTag = "Player";

    [Tooltip("How long after launch before homing activates (seconds)")]
    public float homingDelay = 0.2f;

    [Header("Visual Settings")]
    [Tooltip("Optional trail renderer reference")]
    public TrailRenderer trail;

    private Rigidbody rb;
    private float spawnTime;
    private GameObject owner; // Who fired this projectile
    private Transform target; // Current target being tracked
    private float currentSpeed;
    private bool homingActive = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
        currentSpeed = speed;

        // Configure rigidbody for missile physics
        rb.useGravity = false; // No gravity
        rb.drag = 0; // No air resistance
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Set initial velocity in forward direction
        rb.velocity = transform.forward * speed;

        // Find initial target
        FindNearestTarget();
    }

    void Update()
    {
        // Destroy missile after lifetime expires
        if (Time.time >= spawnTime + lifetime)
        {
            Destroy(gameObject);
        }

        // Activate homing after delay
        if (!homingActive && Time.time >= spawnTime + homingDelay)
        {
            homingActive = true;
        }
    }

    void FixedUpdate()
    {
        if (!homingActive)
            return;

        // Re-acquire target if lost or none found
        if (target == null)
        {
            FindNearestTarget();
        }

        // If we have a target, home in on it
        if (target != null)
        {
            HomeTowardTarget();
        }

        // Accelerate up to max speed
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.fixedDeltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        }

        // Apply velocity in current forward direction
        rb.velocity = transform.forward * currentSpeed;
    }

    void HomeTowardTarget()
    {
        // Calculate direction to target
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // Calculate rotation needed to face target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Smoothly rotate toward target at turn rate
        float rotationStep = turnRate * Time.fixedDeltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
    }

    void FindNearestTarget()
    {
        // Find all objects with target tag
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);

        float closestDistance = detectionRadius;
        Transform closestTarget = null;

        foreach (GameObject potentialTarget in potentialTargets)
        {
            // Skip the owner
            if (potentialTarget == owner)
                continue;

            // Check distance
            float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = potentialTarget.transform;
            }
        }

        target = closestTarget;

        if (target != null)
        {
            Debug.Log($"Homing missile locked onto: {target.name}");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Don't hit the UFO that fired this projectile
        if (collision.gameObject == owner)
            return;

        // Check what we hit
        if (collision.gameObject.CompareTag("Player"))
        {
            // Hit another UFO - deal damage (future health system)
            Debug.Log($"Homing missile hit {collision.gameObject.name} for {damage} damage!");

            // TODO: Apply damage to target's health system
            // var health = collision.gameObject.GetComponent<UFOHealth>();
            // if (health != null) health.TakeDamage(damage);
        }

        // Destroy missile on any collision (wall, floor, player)
        Destroy(gameObject);
    }

    /// <summary>
    /// Set who fired this missile (to prevent self-hits)
    /// </summary>
    public void SetOwner(GameObject ownerObject)
    {
        owner = ownerObject;
    }

    // Debug visualization in editor
    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw line to current target
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
