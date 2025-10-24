using UnityEngine;

/// <summary>
/// Continuous laser beam weapon that fires straight from UFO
/// Beam persists for duration, rotates with UFO, deals continuous damage
/// </summary>
public class LaserWeapon : MonoBehaviour
{
    [Header("Laser Settings")]
    [Tooltip("How long the laser beam stays active (seconds)")]
    public float beamDuration = 2f;

    [Tooltip("Maximum range of the laser beam")]
    public float beamRange = 100f;

    [Tooltip("Width of the laser beam at the start (near UFO)")]
    public float beamStartWidth = 0.5f;

    [Tooltip("Width of the laser beam at the end (far from UFO)")]
    public float beamEndWidth = 12f;

    [Tooltip("Damage dealt when laser hits target (flat damage for entire beam duration)")]
    public int damage = 1;

    [Tooltip("Cooldown after laser ends before can fire again (seconds)")]
    public float cooldown = 1f;

    [Header("Visual Settings")]
    [Tooltip("Color of the laser beam")]
    public Color beamColor = Color.red;

    [Tooltip("Material for the laser beam (uses default if not assigned)")]
    public Material beamMaterial;

    [Tooltip("How smoothly the laser rotates to follow aiming (higher = faster)")]
    public float aimSmoothSpeed = 10f;

    [Header("Audio (Optional)")]
    [Tooltip("Sound played when laser fires")]
    public AudioClip fireSound;

    [Tooltip("Looping sound while laser is active")]
    public AudioClip beamSound;

    private LineRenderer lineRenderer;
    private AudioSource audioSource;
    private bool isActive = false;
    private float beamEndTime;
    private float cooldownEndTime;
    private Transform firePoint; // Optional fire point
    private GameObject owner; // The UFO firing this
    private UFOController ufoController; // For aiming direction
    private Vector3 currentLaserDirection; // Smoothed laser direction
    private GameObject lastHitTarget; // Track last hit target to deal damage only once per beam activation

    void Start()
    {
        owner = gameObject;
        ufoController = GetComponent<UFOController>();
        currentLaserDirection = transform.forward; // Initialize with forward direction

        // Try to find fire point (optional)
        firePoint = transform.Find("FirePoint");

        // Create LineRenderer for visual beam
        SetupLineRenderer();

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.loop = false;
    }

    void Update()
    {
        // Check for fire input
        // Using Fire3 for laser (can be changed to whatever input you want)
        if (Input.GetButtonDown("Fire3") || Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            TryFire();
        }

        // Update laser if active
        if (isActive)
        {
            UpdateLaser();

            // Check if beam duration expired
            if (Time.time >= beamEndTime)
            {
                DeactivateLaser();
            }
        }
    }

    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = beamStartWidth;
        lineRenderer.endWidth = beamEndWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;

        // Make it a 3D tube - use View alignment but with high quality segments
        lineRenderer.alignment = LineAlignment.View; // Billboard, but we'll make it cylindrical with segments
        lineRenderer.textureMode = LineTextureMode.Tile;

        // These create the cylindrical shape - MORE vertices = rounder tube
        lineRenderer.numCornerVertices = 16; // More vertices for smoother circle
        lineRenderer.numCapVertices = 16; // Rounded end caps
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; // Cast shadows for 3D effect
        lineRenderer.receiveShadows = true;

        // CRITICAL: Tell Unity to generate a proper cylinder mesh
        lineRenderer.useWorldSpace = true;
        lineRenderer.generateLightingData = true;

        // Set material and color
        if (beamMaterial != null)
        {
            lineRenderer.material = beamMaterial;
        }
        else
        {
            // Use Unlit shader for bright laser
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        }

        lineRenderer.startColor = beamColor;
        lineRenderer.endColor = beamColor;

        // Make it glow
        lineRenderer.material.EnableKeyword("_EMISSION");
        lineRenderer.material.SetColor("_EmissionColor", beamColor * 2f);
    }

    public bool TryFire()
    {
        // Check if already active
        if (isActive)
            return false;

        // Check cooldown
        if (Time.time < cooldownEndTime)
        {
            return false;
        }

        // Activate laser
        ActivateLaser();
        return true;
    }

    void ActivateLaser()
    {
        isActive = true;
        beamEndTime = Time.time + beamDuration;
        lineRenderer.enabled = true;
        lastHitTarget = null; // Reset damage tracking for new beam

        // Initialize laser direction to current aim direction (prevents sweep-in)
        if (ufoController != null)
        {
            Quaternion aimDirection = ufoController.GetAimDirection();
            currentLaserDirection = aimDirection * Vector3.forward;
        }
        else
        {
            currentLaserDirection = transform.forward;
        }

        // Play fire sound
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }

        // Play looping beam sound
        if (audioSource != null && beamSound != null)
        {
            audioSource.clip = beamSound;
            audioSource.loop = true;
            audioSource.Play();
        }

    }

    void UpdateLaser()
    {
        // Update beam width dynamically (allows changing in Inspector during play)
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = beamStartWidth;
            lineRenderer.endWidth = beamEndWidth;
        }

        // Get start position (offset in front of UFO, not at center)
        Vector3 startPos;
        if (firePoint != null)
        {
            startPos = firePoint.position;
        }
        else
        {
            // Start 4 units in front of UFO to clear the UFO model completely
            startPos = transform.position + currentLaserDirection * 4f;
        }

        // Get target aiming direction from UFO (includes vertical pitch based on velocity)
        Vector3 targetDirection;
        if (ufoController != null)
        {
            // Use UFO's aiming system (same as weapons - includes vertical pitch)
            Quaternion aimDirection = ufoController.GetAimDirection();
            targetDirection = aimDirection * Vector3.forward;
        }
        else
        {
            // Fallback to simple forward direction
            targetDirection = transform.forward;
        }

        // Smoothly interpolate current direction toward target direction
        currentLaserDirection = Vector3.Slerp(currentLaserDirection, targetDirection, aimSmoothSpeed * Time.deltaTime);
        currentLaserDirection.Normalize(); // Ensure it stays normalized

        // Use the smoothed direction for the laser
        Vector3 direction = currentLaserDirection;

        // Cast ray to find end position
        RaycastHit hit;
        Vector3 endPos;
        float actualDistance;

        if (Physics.Raycast(startPos, direction, out hit, beamRange))
        {
            // Hit something - beam ends at hit point
            endPos = hit.point;
            actualDistance = Vector3.Distance(startPos, endPos);

            // Check if we hit a UFO
            if (hit.collider.CompareTag("Player") && hit.collider.gameObject != owner)
            {
                // Deal damage
                ApplyDamage(hit.collider.gameObject);
            }
        }
        else
        {
            // Nothing hit - beam goes to max range
            endPos = startPos + direction * beamRange;
            actualDistance = beamRange;
        }

        // Scale end width based on actual distance traveled
        // Width grows proportionally to distance (not compressed when hitting close objects)
        float distanceRatio = actualDistance / beamRange; // 0 to 1
        float scaledEndWidth = Mathf.Lerp(beamStartWidth, beamEndWidth, distanceRatio);

        // Update line renderer with scaled width
        lineRenderer.startWidth = beamStartWidth;
        lineRenderer.endWidth = scaledEndWidth; // Use scaled width instead of full beamEndWidth
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    void ApplyDamage(GameObject target)
    {
        // Only deal damage once per beam activation
        if (target == lastHitTarget)
            return;

        lastHitTarget = target;

        // Deal damage
        UFOHealth health = target.GetComponent<UFOHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Trigger wobble effect
        UFOHitEffect hitEffect = target.GetComponent<UFOHitEffect>();
        if (hitEffect != null)
        {
            hitEffect.TriggerWobble();
        }

        Debug.Log($"[LASER] Hit {target.name} for {damage} damage");
    }

    void DeactivateLaser()
    {
        isActive = false;
        lineRenderer.enabled = false;
        cooldownEndTime = Time.time + cooldown;

        // Stop beam sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

    }

    // Public method for external triggering (alternative to input check)
    public bool IsActive()
    {
        return isActive;
    }

    public bool CanFire()
    {
        return !isActive && Time.time >= cooldownEndTime;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (isActive)
        {
            Gizmos.color = beamColor;
            Vector3 startPos = (firePoint != null) ? firePoint.position : transform.position;
            Gizmos.DrawRay(startPos, transform.forward * beamRange);
        }
    }
}
