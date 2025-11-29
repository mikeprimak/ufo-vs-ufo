using UnityEngine;

/// <summary>
/// Proximity missile that flies straight forward
/// Explodes when it gets within blast range of an enemy UFO (proximity detonation)
/// Also explodes on direct collision or when lifetime expires
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Speed of the projectile")]
    public float speed = 75f;

    [Tooltip("How long before projectile destroys itself (seconds)")]
    public float lifetime = 5f;

    [Tooltip("Damage dealt on direct hit")]
    public int damage = 1;

    [Header("Explosion Settings")]
    [Tooltip("Explosion blast radius (0 = no explosion)")]
    public float blastRadius = 20f;

    [Tooltip("Explosion damage to UFOs in blast radius")]
    public int explosionDamage = 1;

    [Tooltip("Explosion visual effect prefab (optional)")]
    public GameObject explosionPrefab;

    [Tooltip("Explosion sound (optional)")]
    public AudioClip explosionSound;

    [Header("Proximity Settings")]
    [Tooltip("Distance at which missile explodes near enemies (0 = use blastRadius, recommend 50-80)")]
    public float proximityTriggerDistance = 20f;

    [Tooltip("How often to check proximity (seconds, lower = more responsive but higher CPU)")]
    public float proximityCheckInterval = 0.1f;

    [Tooltip("Tag to search for targets (default: Player)")]
    public string targetTag = "Player";

    [Header("Visual Settings")]
    [Tooltip("Optional trail renderer reference")]
    public TrailRenderer trail;

    [Header("Trajectory Indicator")]
    [Tooltip("Show dotted line indicating missile path")]
    public bool showTrajectory = true;

    [Tooltip("Color of trajectory dots")]
    public Color trajectoryColor = Color.red;

    [Tooltip("Size of each trajectory dot")]
    public float dotSize = 0.5f;

    [Tooltip("Spacing between trajectory dots")]
    public float dotSpacing = 5f;

    [Tooltip("Layers the trajectory raycast can hit")]
    public LayerMask trajectoryLayers = -1; // Default: all layers

    private Rigidbody rb;
    private GameObject[] trajectoryDots; // Array of dot objects
    private float spawnTime;
    private GameObject owner; // Who fired this projectile
    private GameObject directHitTarget = null; // UFO that was directly hit (skip in explosion)
    private bool hasCollided = false; // Prevent multiple collision events
    private float lastProximityCheck = 0f; // Time of last proximity check
    private string weaponName = "Proximity Missile"; // Weapon name for combat log
    private string explosionWeaponName = "Proximity Missile"; // Explosion name for combat log (same as weapon name)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;

        // Configure rigidbody for projectile physics
        rb.useGravity = false; // Straight flight, no gravity
        rb.drag = 0; // No air resistance
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Fast-moving object

        // Set initial velocity in forward direction
        rb.velocity = transform.forward * speed;

        // Create trajectory indicator
        if (showTrajectory)
        {
            CreateTrajectoryDots();
        }
    }

    void CreateTrajectoryDots()
    {
        // Raycast to find impact point
        float maxDistance = speed * lifetime; // Maximum possible travel distance
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, trajectoryLayers))
        {
            endPoint = hit.point;
        }
        else
        {
            // No hit - extend to max range
            endPoint = transform.position + transform.forward * maxDistance;
        }

        // Calculate distance and number of dots
        float totalDistance = Vector3.Distance(transform.position, endPoint);
        int numDots = Mathf.FloorToInt(totalDistance / dotSpacing);

        if (numDots <= 0)
            return;

        // Create dots array
        trajectoryDots = new GameObject[numDots];

        // Create unlit material for dots
        Material dotMaterial = new Material(Shader.Find("Unlit/Color"));
        dotMaterial.color = trajectoryColor;

        // Spawn dots along the path
        for (int i = 0; i < numDots; i++)
        {
            // Position dot along the line (start from first spacing, not from missile)
            float t = (i + 1) * dotSpacing / totalDistance;
            Vector3 dotPosition = Vector3.Lerp(transform.position, endPoint, t);

            // Create dot sphere
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "TrajectoryDot";
            dot.transform.position = dotPosition;
            dot.transform.localScale = Vector3.one * dotSize;

            // Remove collider (dots shouldn't block anything)
            Collider dotCollider = dot.GetComponent<Collider>();
            if (dotCollider != null)
            {
                Destroy(dotCollider);
            }

            // Apply material
            Renderer dotRenderer = dot.GetComponent<Renderer>();
            if (dotRenderer != null)
            {
                dotRenderer.material = dotMaterial;
                dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dotRenderer.receiveShadows = false;
            }

            trajectoryDots[i] = dot;
        }
    }

    void CleanupTrajectoryDots()
    {
        if (trajectoryDots != null)
        {
            foreach (GameObject dot in trajectoryDots)
            {
                if (dot != null)
                {
                    Destroy(dot);
                }
            }
            trajectoryDots = null;
        }
    }

    void OnDestroy()
    {
        CleanupTrajectoryDots();
    }

    void Update()
    {
        // Explode when lifetime expires
        if (Time.time >= spawnTime + lifetime)
        {
            Explode();
        }

        // Check proximity at intervals (performance optimization)
        if (Time.time >= lastProximityCheck + proximityCheckInterval)
        {
            lastProximityCheck = Time.time;
            CheckProximity();
        }

        // Remove trajectory dots that missile has passed
        if (showTrajectory && trajectoryDots != null)
        {
            UpdateTrajectoryDots();
        }
    }

    void UpdateTrajectoryDots()
    {
        for (int i = 0; i < trajectoryDots.Length; i++)
        {
            GameObject dot = trajectoryDots[i];
            if (dot == null)
                continue;

            // Check if missile has passed this dot
            // Dot product: positive = ahead, negative = behind
            Vector3 toDot = dot.transform.position - transform.position;
            float dotProduct = Vector3.Dot(toDot, transform.forward);

            if (dotProduct < 0)
            {
                // Dot is behind missile - destroy it
                Destroy(dot);
                trajectoryDots[i] = null;
            }
        }
    }

    void CheckProximity()
    {
        // Use blastRadius as default proximity distance if not specified
        float proximityDistance = proximityTriggerDistance > 0f ? proximityTriggerDistance : blastRadius;

        // Skip proximity check if distance is 0 or negative
        if (proximityDistance <= 0f)
            return;

        // Find all objects with target tag
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);

        foreach (GameObject potentialTarget in potentialTargets)
        {
            // Get the root GameObject (in case we hit a child object)
            GameObject rootTarget = potentialTarget.transform.root.gameObject;

            // Skip the owner (compare root objects)
            if (rootTarget == owner || rootTarget == owner?.transform.root.gameObject)
                continue;

            // Check distance
            float distance = Vector3.Distance(transform.position, rootTarget.transform.position);

            // Explode if within proximity range
            if (distance <= proximityDistance)
            {
                Explode();
                return;
            }
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

            // Hit another UFO - deal direct hit damage with kill attribution
            UFOHealth health = rootObject.GetComponent<UFOHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, owner, weaponName); // Pass owner and weapon name
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
        // Skip explosion if blast radius is 0
        if (blastRadius <= 0f)
        {
            Destroy(gameObject);
            return;
        }

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

                // Deal explosion damage with kill attribution
                UFOHealth health = rootUFO.GetComponent<UFOHealth>();
                if (health != null)
                {
                    health.TakeDamage(explosionDamage, owner, explosionWeaponName); // Pass owner and weapon name
                }

                float distance = Vector3.Distance(transform.position, rootUFO.transform.position);

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

        // Destroy projectile
        Destroy(gameObject);
    }

    /// <summary>
    /// Set who fired this projectile (to prevent self-hits)
    /// </summary>
    public void SetOwner(GameObject ownerObject)
    {
        owner = ownerObject;
    }

    /// <summary>
    /// Set custom weapon names for combat log (e.g., "Laser Burst" instead of "Missile")
    /// </summary>
    public void SetWeaponNames(string directHitName, string explosionName)
    {
        weaponName = directHitName;
        explosionWeaponName = explosionName;
    }
}
