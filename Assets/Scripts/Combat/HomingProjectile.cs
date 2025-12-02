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
    public float speed = 180f;

    [Tooltip("How long before missile destroys itself (seconds)")]
    public float lifetime = 8f;

    [Tooltip("Damage dealt on direct hit")]
    public int damage = 1;

    [Header("Explosion Settings")]
    [Tooltip("Explosion blast radius")]
    public float blastRadius = 10f;

    [Header("Proximity Settings")]
    [Tooltip("Distance at which missile explodes near enemies")]
    public float proximityTriggerDistance = 2f;

    [Tooltip("How often to check proximity (seconds)")]
    public float proximityCheckInterval = 0.1f;

    [Tooltip("Delay before proximity detection activates (seconds) - prevents self-hits at launch")]
    public float proximityArmingDelay = 0.3f;

    [Tooltip("Explosion damage to UFOs in blast radius")]
    public int explosionDamage = 1;

    [Tooltip("Explosion visual effect prefab (optional)")]
    public GameObject explosionPrefab;

    [Tooltip("Explosion sound (optional)")]
    public AudioClip explosionSound;

    [Header("Homing Settings")]
    [Tooltip("How quickly missile turns toward target (degrees per second)")]
    public float turnRate = 90f;

    [Tooltip("How quickly missile accelerates toward max speed")]
    public float acceleration = 100f;

    [Tooltip("Maximum speed the missile can reach")]
    public float maxSpeed = 200f;

    [Tooltip("Detection radius to find targets")]
    public float detectionRadius = 100f;

    [Tooltip("Tag to search for targets (default: Player)")]
    public string targetTag = "Player";

    [Tooltip("How long after launch before homing activates (seconds)")]
    public float homingDelay = 0.2f;

    [Header("U-Turn Settings")]
    [Tooltip("Enable U-turn behavior when no target is ahead")]
    public bool enableUTurn = true;

    [Tooltip("Minimum speed during U-turn (slows down to this)")]
    public float uTurnMinSpeed = 10f;

    [Tooltip("How quickly missile decelerates for U-turn")]
    public float uTurnDeceleration = 60f;

    [Tooltip("Turn rate multiplier during U-turn (faster turning)")]
    public float uTurnTurnMultiplier = 2f;

    [Tooltip("Angle threshold to consider U-turn complete (degrees from 180)")]
    public float uTurnCompleteAngle = 30f;

    [Header("Visual Settings")]
    [Tooltip("Optional trail renderer reference")]
    public TrailRenderer trail;

    [Tooltip("Auto-create visual from primitives")]
    public bool createMissileVisual = true;

    [Tooltip("Use photon torpedo style (red pulsing orb) instead of missile shape")]
    public bool usePhotonTorpedoStyle = true;

    [Tooltip("Color of the missile/torpedo")]
    public Color missileColor = new Color(1f, 0.3f, 0.1f); // Red-orange for photon torpedo

    [Tooltip("Overall scale of the visual")]
    public float missileScale = 2f;

    [Tooltip("Enable particle trail behind missile")]
    public bool enableParticleTrail = true;

    [Tooltip("Color of the particle trail")]
    public Color trailColor = new Color(1f, 0.4f, 0.1f, 0.6f); // Orange-red trail for torpedo

    [Header("Photon Torpedo Settings")]
    [Tooltip("Pulse speed (oscillations per second)")]
    public float pulseSpeed = 3f;

    [Tooltip("How much the torpedo pulses (0-1)")]
    public float pulseAmount = 0.3f;

    [Tooltip("Inner glow intensity")]
    public float glowIntensity = 2f;

    // Photon torpedo visual references
    private GameObject torpedoOrb;
    private GameObject torpedoGlow;
    private Material torpedoMaterial;
    private float baseTorpedoScale;

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

    // U-turn state
    private bool isDoingUTurn = false;
    private Vector3 uTurnStartDirection; // Direction we were facing when U-turn started
    private bool hasSlowedForUTurn = false; // Track if we've reached min speed

    // Proximity detection
    private float lastProximityCheck = 0f;
    private GameObject ownerRoot = null; // Cached root of owner for reliable comparisons

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

        // Create visual from primitives
        if (createMissileVisual)
        {
            if (usePhotonTorpedoStyle)
            {
                CreatePhotonTorpedoVisual();
            }
            else
            {
                CreateMissileVisual();
            }
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

    /// <summary>
    /// Create Star Trek TNG-style photon torpedo visual (red pulsing orb of light)
    /// </summary>
    void CreatePhotonTorpedoVisual()
    {
        // Hide any existing mesh on the root object
        MeshRenderer existingRenderer = GetComponent<MeshRenderer>();
        if (existingRenderer != null)
        {
            existingRenderer.enabled = false;
        }

        float scale = missileScale;
        baseTorpedoScale = scale;

        // === OUTER GLOW: Larger, semi-transparent sphere (render first/behind) ===
        torpedoGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        torpedoGlow.name = "TorpedoGlow";
        torpedoGlow.transform.SetParent(transform);
        torpedoGlow.transform.localPosition = Vector3.zero;
        torpedoGlow.transform.localScale = Vector3.one * scale * 2.5f; // Larger than core
        Destroy(torpedoGlow.GetComponent<Collider>());

        // Use Sprites/Default shader which supports transparency in URP
        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        Color glowColor = new Color(1f, 0.2f, 0f, 0.25f); // Red-orange, semi-transparent
        glowMat.color = glowColor;
        glowMat.renderQueue = 3000; // Transparent queue
        torpedoGlow.GetComponent<Renderer>().material = glowMat;
        torpedoGlow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === MAIN ORB: Bright red-orange sphere ===
        torpedoOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        torpedoOrb.name = "TorpedoOrb";
        torpedoOrb.transform.SetParent(transform);
        torpedoOrb.transform.localPosition = Vector3.zero;
        torpedoOrb.transform.localScale = Vector3.one * scale;
        Destroy(torpedoOrb.GetComponent<Collider>());

        // Create bright unlit material for the core - use Sprites/Default for color
        torpedoMaterial = new Material(Shader.Find("Sprites/Default"));
        torpedoMaterial.color = missileColor; // Red-orange
        torpedoMaterial.renderQueue = 3001; // Render on top of glow
        torpedoOrb.GetComponent<Renderer>().material = torpedoMaterial;
        torpedoOrb.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === INNER CORE: Smaller, brighter hot center ===
        GameObject innerCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        innerCore.name = "TorpedoCore";
        innerCore.transform.SetParent(transform);
        innerCore.transform.localPosition = Vector3.zero;
        innerCore.transform.localScale = Vector3.one * scale * 0.4f; // Smaller than main orb
        Destroy(innerCore.GetComponent<Collider>());

        // Bright yellow-white core
        Material coreMat = new Material(Shader.Find("Sprites/Default"));
        coreMat.color = new Color(1f, 0.95f, 0.8f); // Yellow-white hot center
        coreMat.renderQueue = 3002; // Render on top
        innerCore.GetComponent<Renderer>().material = coreMat;
        innerCore.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === PARTICLE TRAIL: Red-orange energy trail ===
        if (enableParticleTrail)
        {
            CreateTorpedoTrail(scale);
        }
    }

    /// <summary>
    /// Create energy trail for photon torpedo (different from missile smoke trail)
    /// </summary>
    void CreateTorpedoTrail(float scale)
    {
        GameObject trailObj = new GameObject("TorpedoTrail");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = new Vector3(0f, 0f, -0.5f * scale); // Slightly behind center

        ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0f;
        main.startSize = 0.8f * scale;
        main.startColor = trailColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f * scale;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        // Bright red-orange at start, fade to dark red
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(new Color(0.8f, 0.1f, 0f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Particle material
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

        // Animate photon torpedo pulsing
        if (usePhotonTorpedoStyle && torpedoOrb != null)
        {
            UpdateTorpedoPulse();
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

        // Check proximity at intervals (performance optimization)
        if (Time.time >= lastProximityCheck + proximityCheckInterval)
        {
            lastProximityCheck = Time.time;
            CheckProximity();
        }
    }

    /// <summary>
    /// Animate the photon torpedo pulsing effect
    /// </summary>
    void UpdateTorpedoPulse()
    {
        // Calculate pulse using sine wave
        float pulse = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
        float scaleMod = 1f + (pulse * pulseAmount);

        // Pulse the main orb scale
        if (torpedoOrb != null)
        {
            torpedoOrb.transform.localScale = Vector3.one * baseTorpedoScale * scaleMod;
        }

        // Pulse the glow with opposite phase (expands when core shrinks)
        if (torpedoGlow != null)
        {
            float glowScaleMod = 1f + (-pulse * pulseAmount * 0.5f);
            torpedoGlow.transform.localScale = Vector3.one * baseTorpedoScale * 1.8f * glowScaleMod;
        }

        // Pulse the color brightness
        if (torpedoMaterial != null)
        {
            float brightness = 1f + (pulse * 0.3f);
            Color pulsedColor = new Color(
                Mathf.Clamp01(missileColor.r * brightness),
                Mathf.Clamp01(missileColor.g * brightness),
                Mathf.Clamp01(missileColor.b * brightness)
            );
            torpedoMaterial.color = pulsedColor;
        }
    }

    void CheckProximity()
    {
        // Skip if proximity distance not set
        if (proximityTriggerDistance <= 0f)
            return;

        // Don't check proximity until arming delay has passed (prevents self-hits at launch)
        if (Time.time < spawnTime + proximityArmingDelay)
            return;

        // Find all objects with target tag
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);

        foreach (GameObject potentialTarget in potentialTargets)
        {
            // Get the root GameObject (in case we hit a child object)
            GameObject rootTarget = potentialTarget.transform.root.gameObject;

            // Skip the owner (check both owner and cached ownerRoot for safety)
            if (rootTarget == owner || rootTarget == ownerRoot)
                continue;

            // Skip dead UFOs
            UFOHealth health = rootTarget.GetComponent<UFOHealth>();
            if (health != null && health.IsDead())
                continue;

            // Check distance
            float distance = Vector3.Distance(transform.position, rootTarget.transform.position);

            // Explode if within proximity range
            if (distance <= proximityTriggerDistance)
            {
                Explode();
                return;
            }
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
        else
        {
            // Check if current target became invisible - lose lock
            InvisibilityItem targetInvis = target.root.GetComponent<InvisibilityItem>();
            if (targetInvis != null && targetInvis.IsInvisible())
            {
                target = null; // Lose target
                FindNearestTarget(); // Try to find another
            }
        }

        // If we have a target, home in on it and cancel any U-turn
        if (target != null)
        {
            // Cancel U-turn if we found a target
            if (isDoingUTurn)
            {
                isDoingUTurn = false;
                hasSlowedForUTurn = false;
            }

            HomeTowardTarget();

            // Accelerate up to max speed
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += acceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }
        }
        else if (enableUTurn)
        {
            // No target found - check if we should do a U-turn
            HandleUTurn();
        }
        else
        {
            // U-turn disabled, just keep accelerating forward
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += acceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }
        }

        // Apply velocity in current forward direction
        rb.velocity = transform.forward * currentSpeed;
    }

    void HandleUTurn()
    {
        // Check if there's any potential target behind us
        Transform behindTarget = FindAnyTarget();

        if (behindTarget == null)
        {
            // No targets anywhere - just fly straight
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += acceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }
            return;
        }

        // Check if target is behind us (more than 90 degrees off forward)
        Vector3 toTarget = (behindTarget.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, toTarget);

        // If target is roughly ahead (within 90 degrees), don't U-turn, just seek normally
        if (angleToTarget < 90f)
        {
            // Target is ahead but we don't have line-of-sight (or outside detection range)
            // Just keep going and hope to reacquire
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += acceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }
            return;
        }

        // Target is behind us - initiate or continue U-turn
        if (!isDoingUTurn)
        {
            // Start the U-turn
            isDoingUTurn = true;
            uTurnStartDirection = transform.forward;
            hasSlowedForUTurn = false;
        }

        // Phase 1: Slow down
        if (!hasSlowedForUTurn)
        {
            currentSpeed -= uTurnDeceleration * Time.fixedDeltaTime;
            if (currentSpeed <= uTurnMinSpeed)
            {
                currentSpeed = uTurnMinSpeed;
                hasSlowedForUTurn = true;
            }
        }

        // Phase 2: Turn toward target (faster turn rate during U-turn)
        Vector3 directionToTarget = toTarget;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        float rotationStep = turnRate * uTurnTurnMultiplier * Time.fixedDeltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);

        // Check if U-turn is complete (now facing roughly toward target)
        float currentAngleToTarget = Vector3.Angle(transform.forward, toTarget);
        if (currentAngleToTarget < uTurnCompleteAngle)
        {
            // U-turn complete - accelerate and try to acquire target normally
            isDoingUTurn = false;
            hasSlowedForUTurn = false;

            // Speed back up
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += acceleration * Time.fixedDeltaTime;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }
        }
    }

    /// <summary>
    /// Find any valid target regardless of line-of-sight (for U-turn decision)
    /// </summary>
    Transform FindAnyTarget()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);

        float nearestDistance = float.MaxValue;
        Transform nearestTarget = null;

        foreach (GameObject potentialTarget in potentialTargets)
        {
            // Get the root object for reliable comparison
            GameObject rootTarget = potentialTarget.transform.root.gameObject;

            // Skip the owner (check both owner and ownerRoot)
            if (rootTarget == owner || rootTarget == ownerRoot)
                continue;

            // Skip dead UFOs
            UFOHealth health = potentialTarget.GetComponent<UFOHealth>();
            if (health != null && health.IsDead())
                continue;

            // Skip invisible UFOs (can't be tracked by homing missiles)
            InvisibilityItem invisibility = rootTarget.GetComponent<InvisibilityItem>();
            if (invisibility != null && invisibility.IsInvisible())
                continue;

            float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);

            // Use larger detection radius for U-turn decisions
            if (distance > detectionRadius * 2f)
                continue;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = potentialTarget.transform;
            }
        }

        return nearestTarget;
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
            // Get the root object for reliable comparison
            GameObject rootTarget = potentialTarget.transform.root.gameObject;

            // Skip the owner (check both owner and ownerRoot)
            if (rootTarget == owner || rootTarget == ownerRoot)
                continue;

            // Skip invisible UFOs (can't be tracked by homing missiles)
            InvisibilityItem invisibility = rootTarget.GetComponent<InvisibilityItem>();
            if (invisibility != null && invisibility.IsInvisible())
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

        // Always get the root object first (handles hitting child objects like UFO_Body)
        GameObject rootObject = collision.gameObject.transform.root.gameObject;

        // Don't hit the UFO that fired this projectile (check both owner and ownerRoot)
        if (rootObject == owner || rootObject == ownerRoot)
            return;

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

                // Skip the owner (check both owner and ownerRoot)
                if (rootUFO == owner || rootUFO == ownerRoot)
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
        // Cache the root object for reliable comparisons
        if (owner != null)
        {
            ownerRoot = owner.transform.root.gameObject;
        }
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

        // Show U-turn state
        if (isDoingUTurn)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 3f);
            // Draw original direction we're turning from
            Gizmos.DrawRay(transform.position, uTurnStartDirection * 10f);
        }
    }
}
