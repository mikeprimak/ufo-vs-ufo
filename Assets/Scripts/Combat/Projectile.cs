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
    [Tooltip("Starting speed of the projectile (should match UFO max speed)")]
    public float startSpeed = 90f;

    [Tooltip("Maximum speed after acceleration")]
    public float maxSpeed = 300f;

    [Tooltip("How quickly missile accelerates (units/sec²)")]
    public float acceleration = 150f;

    [Tooltip("How long before projectile destroys itself (seconds)")]
    public float lifetime = 5f;

    [Tooltip("Damage dealt on direct hit")]
    public int damage = 1;

    [Header("Explosion Settings")]
    [Tooltip("Explosion blast radius (0 = no explosion)")]
    public float blastRadius = 10f;

    [Tooltip("Explosion damage to UFOs in blast radius")]
    public int explosionDamage = 1;

    [Tooltip("Explosion visual effect prefab (optional)")]
    public GameObject explosionPrefab;

    [Tooltip("Explosion sound (optional)")]
    public AudioClip explosionSound;

    [Header("Proximity Settings")]
    [Tooltip("Distance at which missile explodes near enemies (0 = use blastRadius)")]
    public float proximityTriggerDistance = 10f;

    [Tooltip("How often to check proximity (seconds, lower = more responsive but higher CPU)")]
    public float proximityCheckInterval = 0.1f;

    [Tooltip("Tag to search for targets (default: Player)")]
    public string targetTag = "Player";

    [Header("Visual Settings")]
    [Tooltip("Optional trail renderer reference")]
    public TrailRenderer trail;

    [Tooltip("Auto-create missile shape from primitives")]
    public bool createMissileVisual = true;

    [Tooltip("Create laser bolt visual instead of missile (Star Wars style)")]
    public bool createLaserVisual = false;

    [Tooltip("Color of the entire missile (body, nose, fins)")]
    public Color missileColor = new Color(0.6f, 0.6f, 0.6f); // Uniform grey

    [Tooltip("Color of the laser bolt (for laser visual)")]
    public Color laserColor = new Color(0.3f, 1f, 0f); // Green laser

    [Tooltip("Overall scale of the missile visual")]
    public float missileScale = 2f;

    [Tooltip("Length of the laser bolt")]
    public float laserLength = 3f;

    [Tooltip("Width of the laser bolt")]
    public float laserWidth = 0.3f;

    [Tooltip("Enable particle trail behind missile")]
    public bool enableParticleTrail = true;

    [Tooltip("Color of the particle trail")]
    public Color trailColor = new Color(0.9f, 0.9f, 0.9f, 0.5f); // Light grey, almost white

    [Header("Trajectory Indicator")]
    [Tooltip("Show dotted line indicating missile path")]
    public bool showTrajectory = true;

    [Tooltip("Color of trajectory dots")]
    public Color trajectoryColor = Color.red;

    [Tooltip("Size of each trajectory dot")]
    public float dotSize = 0.2f;

    [Tooltip("Spacing between trajectory dots")]
    public float dotSpacing = 10f;

    [Tooltip("Layers the trajectory raycast can hit")]
    public LayerMask trajectoryLayers = -1; // Default: all layers

    private Rigidbody rb;
    private GameObject[] trajectoryDots; // Array of dot objects
    private float spawnTime;
    private float currentSpeed; // Current speed (accelerates over time)
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
        currentSpeed = startSpeed; // Start at UFO speed

        // Configure rigidbody for projectile physics
        rb.useGravity = false; // Straight flight, no gravity
        rb.drag = 0; // No air resistance
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Fast-moving object

        // Set initial velocity in forward direction
        rb.velocity = transform.forward * currentSpeed;

        // Create visual from primitives
        if (createLaserVisual)
        {
            CreateLaserVisual();
        }
        else if (createMissileVisual)
        {
            CreateMissileVisual();
        }

        // Create trajectory indicator
        if (showTrajectory)
        {
            CreateTrajectoryDots();
        }
    }

    void CreateMissileVisual()
    {
        // Hide any existing mesh on the root object (e.g., default sphere from prefab)
        MeshRenderer existingRenderer = GetComponent<MeshRenderer>();
        if (existingRenderer != null)
        {
            existingRenderer.enabled = false;
        }

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
        // Long and thin: 2x length for sleek missile look
        body.transform.localScale = new Vector3(0.3f * scale, 2f * scale, 0.3f * scale);
        Destroy(body.GetComponent<Collider>()); // No extra colliders
        body.GetComponent<Renderer>().material = missileMat;
        body.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === NOSE CONE: Pointed tip using scaled sphere ===
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "MissileNose";
        nose.transform.SetParent(transform);
        // Position at front of body
        nose.transform.localPosition = new Vector3(0f, 0f, 2.1f * scale);
        // Stretched sphere to make a cone shape
        nose.transform.localScale = new Vector3(0.3f * scale, 0.3f * scale, 0.6f * scale);
        Destroy(nose.GetComponent<Collider>());
        nose.GetComponent<Renderer>().material = missileMat;
        nose.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === FINS: 4 fins at the back ===
        float finLength = 0.5f * scale;
        float finWidth = 0.02f * scale;
        float finHeight = 0.35f * scale;
        float finBackOffset = -1.85f * scale; // Position at rear of body
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
        exhaust.transform.localPosition = new Vector3(0f, 0f, -2.05f * scale);
        exhaust.transform.localScale = new Vector3(0.2f * scale, 0.2f * scale, 0.2f * scale);
        Destroy(exhaust.GetComponent<Collider>());
        exhaust.GetComponent<Renderer>().material = missileMat;
        exhaust.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === PARTICLE TRAIL: Smoke/fire trail behind missile ===
        if (enableParticleTrail)
        {
            CreateParticleTrail(scale);
        }
    }

    /// <summary>
    /// Create Star Wars-style laser bolt visual (pew pew pew)
    /// </summary>
    void CreateLaserVisual()
    {
        // Hide any existing mesh on the root object
        MeshRenderer existingRenderer = GetComponent<MeshRenderer>();
        if (existingRenderer != null)
        {
            existingRenderer.enabled = false;
        }

        // Create bright unlit material for laser bolt
        Material laserMat = new Material(Shader.Find("Unlit/Color"));
        laserMat.color = laserColor;

        // === MAIN BOLT: Bright elongated capsule ===
        GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bolt.name = "LaserBolt";
        bolt.transform.SetParent(transform);
        bolt.transform.localPosition = Vector3.zero;
        // Rotate to point forward (capsules are vertical by default)
        bolt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // Long and thin laser bolt
        bolt.transform.localScale = new Vector3(laserWidth, laserLength * 0.5f, laserWidth);
        Destroy(bolt.GetComponent<Collider>());
        bolt.GetComponent<Renderer>().material = laserMat;
        bolt.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void CreateParticleTrail(float scale)
    {
        GameObject trailObj = new GameObject("MissileTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = new Vector3(0f, 0f, -2.1f * scale); // Behind exhaust

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

    void FixedUpdate()
    {
        // Accelerate missile up to max speed
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.fixedDeltaTime;
            if (currentSpeed > maxSpeed)
                currentSpeed = maxSpeed;

            // Update velocity (keep same direction)
            rb.velocity = transform.forward * currentSpeed;
        }
    }

    void CreateTrajectoryDots()
    {
        // Raycast to find impact point
        // Trajectory extends THROUGH UFOs so players can see incoming missiles
        // Use average speed for trajectory estimate (starts at startSpeed, ends at maxSpeed)
        float avgSpeed = (startSpeed + maxSpeed) / 2f;
        float maxDistance = avgSpeed * lifetime; // Maximum possible travel distance

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

        // Spawn explosion visual effect (scaled to match blast radius)
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            // Scale explosion visual to match blast radius (base prefab assumes radius of 20)
            float scaleFactor = blastRadius / 20f;
            explosion.transform.localScale *= scaleFactor;
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
