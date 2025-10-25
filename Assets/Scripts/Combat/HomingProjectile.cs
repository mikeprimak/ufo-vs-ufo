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

    [Tooltip("Damage dealt on direct hit")]
    public int damage = 1;

    [Header("Explosion Settings")]
    [Tooltip("Explosion blast radius")]
    public float blastRadius = 40f;

    [Tooltip("Explosion damage to UFOs in blast radius")]
    public int explosionDamage = 1;

    [Tooltip("Explosion visual effect prefab (optional)")]
    public GameObject explosionPrefab;

    [Tooltip("Explosion sound (optional)")]
    public AudioClip explosionSound;

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
    private GameObject directHitTarget = null; // UFO that was directly hit (skip in explosion)
    private bool hasCollided = false; // Prevent multiple collision events

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
        // Explode when lifetime expires
        if (Time.time >= spawnTime + lifetime)
        {
            Explode();
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

        Debug.Log($"[HOMING] Found {potentialTargets.Length} potential targets with tag '{targetTag}'");

        float bestScore = float.MaxValue;
        Transform bestTarget = null;

        foreach (GameObject potentialTarget in potentialTargets)
        {
            // Skip the owner
            if (potentialTarget == owner)
            {
                Debug.Log($"[HOMING] Skipping owner: {potentialTarget.name}");
                continue;
            }

            // Check distance
            float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);

            if (distance > detectionRadius)
                continue;

            // Calculate angle from missile's forward direction to target
            Vector3 directionToTarget = (potentialTarget.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            Debug.Log($"[HOMING] Checking {potentialTarget.name}, distance: {distance:F1}, angle: {angle:F1}°");

            // Check line of sight - missile can't track through walls
            RaycastHit hit;

            // Start raycast slightly forward to avoid hitting own collider
            Vector3 rayStart = transform.position + directionToTarget * 0.5f;

            if (Physics.Raycast(rayStart, directionToTarget, out hit, distance))
            {
                Debug.Log($"[HOMING] Raycast hit: {hit.collider.gameObject.name} (tag: {hit.collider.tag})");

                // Check if raycast hit the target UFO (or any of its child objects)
                // Check the root GameObject's tag (handles UFO_Visual children)
                if (hit.collider.transform.root.CompareTag(targetTag))
                {
                    // Score based on angle (heavily weighted) and distance
                    // Lower score is better - prioritizes forward targets
                    float angleWeight = 5f; // Heavy bias toward forward targets
                    float distanceWeight = 0.5f; // Distance matters less
                    float score = (angle * angleWeight) + (distance * distanceWeight);

                    Debug.Log($"[HOMING] Valid target! Score: {score:F1} (angle: {angle:F1}°, distance: {distance:F1})");

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = potentialTarget.transform;
                        Debug.Log($"[HOMING] NEW BEST TARGET: {potentialTarget.name}");
                    }
                }
                else
                {
                    Debug.Log($"[HOMING] LOS blocked by: {hit.collider.gameObject.name}");
                }
            }
            else
            {
                Debug.Log($"[HOMING] Raycast hit nothing");
            }
        }

        target = bestTarget;

        if (target != null)
        {
            Debug.Log($"[HOMING] Final target: {target.name}");
        }
        else
        {
            Debug.Log($"[HOMING] No valid target found");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Prevent multiple collision events for same impact
        if (hasCollided)
            return;
        hasCollided = true;

        // Don't hit the UFO that fired this projectile
        if (collision.gameObject == owner)
            return;

        // Always get the root object first
        GameObject rootObject = collision.gameObject.transform.root.gameObject;

        // Check if we hit a UFO
        if (rootObject.CompareTag("Player"))
        {
            // Store the root UFO that was directly hit (to skip in explosion)
            directHitTarget = rootObject;

            Debug.Log($"[HOMING MISSILE] Direct hit on {rootObject.name}, marking to skip explosion damage");

            // Hit another UFO - deal direct hit damage
            UFOHealth health = rootObject.GetComponent<UFOHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            // Trigger wobble effect
            UFOHitEffect hitEffect = rootObject.GetComponent<UFOHitEffect>();
            if (hitEffect != null)
            {
                hitEffect.TriggerWobble();
            }
        }

        // Explode on any collision (wall, floor, player)
        Explode();
    }

    void Explode()
    {
        Debug.Log($"[HOMING MISSILE] EXPLODING at {transform.position} with radius {blastRadius}");

        // Track which UFOs we've already damaged (to avoid hitting same UFO multiple times)
        System.Collections.Generic.HashSet<GameObject> damagedUFOs = new System.Collections.Generic.HashSet<GameObject>();

        // Find all UFOs in blast radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (Collider hit in hitColliders)
        {
            // Check if we hit the root object or a child with Player tag
            if (hit.transform.root.CompareTag("Player"))
            {
                GameObject rootUFO = hit.transform.root.gameObject;

                // Skip the owner
                if (rootUFO == owner)
                    continue;

                // Skip the directly hit UFO (already took collision damage)
                if (rootUFO == directHitTarget)
                    continue;

                // Skip if we already damaged this UFO
                if (damagedUFOs.Contains(rootUFO))
                    continue;

                // Mark this UFO as damaged
                damagedUFOs.Add(rootUFO);

                // Deal explosion damage
                UFOHealth health = rootUFO.GetComponent<UFOHealth>();
                if (health != null)
                {
                    health.TakeDamage(explosionDamage);
                }

                float distance = Vector3.Distance(transform.position, rootUFO.transform.position);
                Debug.Log($"[HOMING MISSILE] Explosion damaged {rootUFO.name} for {explosionDamage} damage (distance: {distance:F1})");

                // Trigger wobble effect (distance-based intensity)
                UFOHitEffect hitEffect = rootUFO.GetComponent<UFOHitEffect>();
                if (hitEffect != null)
                {
                    float damageFalloff = 1f - (distance / blastRadius);
                    float wobbleIntensity = 15f * damageFalloff;
                    hitEffect.TriggerWobble(wobbleIntensity);
                }

                // Apply explosion knockback force
                Rigidbody targetRb = rootUFO.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    Vector3 explosionDirection = (rootUFO.transform.position - transform.position).normalized;
                    float damageFalloff = 1f - (distance / blastRadius);
                    float explosionForce = 30f * damageFalloff;
                    targetRb.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);
                }
            }
        }

        // Spawn explosion visual effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 5f);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
        }

        // Destroy missile
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
