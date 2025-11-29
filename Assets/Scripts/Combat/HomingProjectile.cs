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
    public float blastRadius = 20f;

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

    [Tooltip("Auto-create missile shape from primitives")]
    public bool createMissileVisual = true;

    [Tooltip("Color of the entire missile (body, nose, fins)")]
    public Color missileColor = new Color(0.8f, 0.2f, 0.2f); // Red

    [Tooltip("Overall scale of the missile visual")]
    public float missileScale = 2f;

    [Tooltip("Enable particle trail behind missile")]
    public bool enableParticleTrail = true;

    [Tooltip("Color of the particle trail")]
    public Color trailColor = new Color(0.9f, 0.9f, 0.9f, 0.5f); // Light grey, almost white

    [Header("Trajectory Indicator")]
    [Tooltip("Show dotted line indicating predicted missile path")]
    public bool showTrajectory = true;

    [Tooltip("Color of trajectory dots")]
    public Color trajectoryColor = new Color(1f, 0.5f, 0f); // Orange for homing

    [Tooltip("Size of each trajectory dot")]
    public float dotSize = 0.5f;

    [Tooltip("Spacing between trajectory dots")]
    public float dotSpacing = 5f;

    [Tooltip("Layers the trajectory raycast can hit")]
    public LayerMask trajectoryLayers = -1; // Default: all layers

    [Tooltip("Maximum number of dots to show")]
    public int maxDots = 30;

    private Rigidbody rb;
    private GameObject[] trajectoryDots; // Array of dot objects
    private Material dotMaterial; // Shared material for all dots
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

        // Create missile visual from primitives
        if (createMissileVisual)
        {
            CreateMissileVisual();
        }

        // Find initial target
        FindNearestTarget();

        // Create trajectory indicator
        if (showTrajectory)
        {
            CreateTrajectoryDots();
        }
    }

    void CreateMissileVisual()
    {
        // Create single unlit material for entire missile
        Material missileMat = new Material(Shader.Find("Unlit/Color"));
        missileMat.color = missileColor;

        float scale = missileScale;

        // === BODY: Long cylinder ===
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "MissileBody";
        body.transform.SetParent(transform);
        body.transform.localPosition = Vector3.zero;
        // Rotate cylinder to point forward (cylinders are vertical by default)
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // Long and thin: 3x length for sleek missile look
        body.transform.localScale = new Vector3(0.3f * scale, 3f * scale, 0.3f * scale);
        Destroy(body.GetComponent<Collider>()); // No extra colliders
        body.GetComponent<Renderer>().material = missileMat;
        body.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === NOSE CONE: Pointed tip using scaled sphere ===
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "MissileNose";
        nose.transform.SetParent(transform);
        // Position at front of longer body
        nose.transform.localPosition = new Vector3(0f, 0f, 3.2f * scale);
        // Stretched sphere to make a cone shape
        nose.transform.localScale = new Vector3(0.3f * scale, 0.3f * scale, 0.6f * scale);
        Destroy(nose.GetComponent<Collider>());
        nose.GetComponent<Renderer>().material = missileMat;
        nose.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === FINS: 4 fins at the back ===
        float finLength = 0.5f * scale;
        float finWidth = 0.02f * scale;
        float finHeight = 0.35f * scale;
        float finBackOffset = -2.8f * scale; // Position at rear of longer body
        float finOutwardOffset = 0.2f * scale;

        for (int i = 0; i < 4; i++)
        {
            GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "MissileFin" + i;
            fin.transform.SetParent(transform);

            // Position fins in a cross pattern (up, down, left, right)
            float angle = i * 90f;
            Vector3 finOffset = Quaternion.Euler(0f, 0f, angle) * new Vector3(0f, finOutwardOffset, 0f);
            fin.transform.localPosition = new Vector3(finOffset.x, finOffset.y, finBackOffset);

            // Rotate fin to stick outward
            fin.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Scale: thin, tall, swept back
            fin.transform.localScale = new Vector3(finWidth, finHeight, finLength);

            Destroy(fin.GetComponent<Collider>());
            fin.GetComponent<Renderer>().material = missileMat;
            fin.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // === EXHAUST: Small sphere at back (same color as missile) ===
        GameObject exhaust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        exhaust.name = "MissileExhaust";
        exhaust.transform.SetParent(transform);
        exhaust.transform.localPosition = new Vector3(0f, 0f, -3.1f * scale);
        exhaust.transform.localScale = new Vector3(0.2f * scale, 0.2f * scale, 0.2f * scale);
        Destroy(exhaust.GetComponent<Collider>());
        exhaust.GetComponent<Renderer>().material = missileMat;
        exhaust.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === PARTICLE TRAIL: Smoke trail behind missile ===
        if (enableParticleTrail)
        {
            CreateParticleTrail(scale);
        }
    }

    void CreateParticleTrail(float scale)
    {
        GameObject trailObj = new GameObject("MissileTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = new Vector3(0f, 0f, -3.2f * scale); // Behind exhaust

        ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 0.5f * scale;
        main.startColor = trailColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f * scale;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f); // Shrink to nothing

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        // Use simple unlit particle material
        var renderer = trailObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = trailColor;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void CreateTrajectoryDots()
    {
        // Create shared material for all dots
        dotMaterial = new Material(Shader.Find("Unlit/Color"));
        dotMaterial.color = trajectoryColor;

        // Create dots array
        trajectoryDots = new GameObject[maxDots];

        // Create dot objects (initially hidden, will be positioned in Update)
        for (int i = 0; i < maxDots; i++)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "HomingTrajectoryDot";
            dot.transform.localScale = Vector3.one * dotSize;

            // Remove collider
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

            dot.SetActive(false); // Start hidden
            trajectoryDots[i] = dot;
        }
    }

    void UpdateTrajectoryDots()
    {
        // Raycast in current direction to find impact point
        // Trajectory extends THROUGH UFOs so players can see incoming missiles
        float maxDistance = currentSpeed * (lifetime - (Time.time - spawnTime));
        maxDistance = Mathf.Max(maxDistance, 10f); // Minimum distance

        Vector3 endPoint = transform.position + transform.forward * maxDistance;

        // Use RaycastAll to find first non-UFO hit (walls, floor, etc.)
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, maxDistance, trajectoryLayers);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // Skip UFOs - trajectory should pass through them
            if (hit.collider.transform.root.CompareTag("Player"))
                continue;

            // Found environment hit
            endPoint = hit.point;
            break;
        }

        // Calculate distance and number of dots needed
        float totalDistance = Vector3.Distance(transform.position, endPoint);
        int numDotsNeeded = Mathf.Min(Mathf.FloorToInt(totalDistance / dotSpacing), maxDots);

        // Position active dots along the path
        for (int i = 0; i < maxDots; i++)
        {
            if (trajectoryDots[i] == null)
                continue;

            if (i < numDotsNeeded)
            {
                // Position dot along the line
                float t = (i + 1) * dotSpacing / totalDistance;
                Vector3 dotPosition = Vector3.Lerp(transform.position, endPoint, t);

                trajectoryDots[i].transform.position = dotPosition;
                trajectoryDots[i].SetActive(true);
            }
            else
            {
                // Hide unused dots
                trajectoryDots[i].SetActive(false);
            }
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

        if (dotMaterial != null)
        {
            Destroy(dotMaterial);
            dotMaterial = null;
        }
    }

    void OnDestroy()
    {
        CleanupTrajectoryDots();
    }

    void Update()
    {
        // Update trajectory indicator every frame
        if (showTrajectory && trajectoryDots != null)
        {
            UpdateTrajectoryDots();
        }

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

        float bestScore = float.MaxValue;
        Transform bestTarget = null;

        foreach (GameObject potentialTarget in potentialTargets)
        {
            // Skip the owner
            if (potentialTarget == owner)
                continue;

            // Check distance
            float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);

            if (distance > detectionRadius)
                continue;

            // Calculate angle from missile's forward direction to target
            Vector3 directionToTarget = (potentialTarget.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            // Check line of sight - missile can't track through walls
            RaycastHit hit;

            // Start raycast slightly forward to avoid hitting own collider
            Vector3 rayStart = transform.position + directionToTarget * 0.5f;

            if (Physics.Raycast(rayStart, directionToTarget, out hit, distance))
            {
                // Check if raycast hit the target UFO (or any of its child objects)
                // Check the root GameObject's tag (handles UFO_Visual children)
                if (hit.collider.transform.root.CompareTag(targetTag))
                {
                    // Score based on angle (heavily weighted) and distance
                    // Lower score is better - prioritizes forward targets
                    float angleWeight = 5f; // Heavy bias toward forward targets
                    float distanceWeight = 0.5f; // Distance matters less
                    float score = (angle * angleWeight) + (distance * distanceWeight);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = potentialTarget.transform;
                    }
                }
            }
        }

        target = bestTarget;
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
                health.TakeDamage(damage, owner, "Homing Missile"); // Pass owner and weapon name
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
                    health.TakeDamage(explosionDamage, owner, "Homing Missile"); // Pass owner and weapon name
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
