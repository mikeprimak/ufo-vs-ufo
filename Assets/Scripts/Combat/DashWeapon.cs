using UnityEngine;

/// <summary>
/// Dash weapon - gives UFO a speed boost and allows ramming damage
/// Creates a blue force field visual in front of the UFO
/// </summary>
public class DashWeapon : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("Forward speed multiplier during dash (only affects forward/backward movement)")]
    public float forwardSpeedMultiplier = 3f;

    [Tooltip("How long the dash lasts (seconds)")]
    public float dashDuration = 6f;

    [Tooltip("Ramming damage dealt when hitting other UFOs")]
    public int rammingDamage = 1;

    [Header("Force Field Visual")]
    [Tooltip("Force field object (will be created if null)")]
    public GameObject forceFieldVisual;

    [Tooltip("Color of the force field")]
    public Color forceFieldColor = new Color(0.3f, 0.5f, 1f, 0.3f); // Blue semi-transparent

    [Tooltip("Distance in front of UFO to place force field")]
    public float forceFieldDistance = 2f;

    [Tooltip("Size of the force field")]
    public float forceFieldSize = 4f;

    [Header("Audio (Optional)")]
    [Tooltip("Sound played when dash activates")]
    public AudioClip dashSound;

    [Tooltip("Looping sound while dashing")]
    public AudioClip dashLoopSound;

    private bool isDashing = false;
    private float dashEndTime;
    private UFOController ufoController;
    private Rigidbody rb;
    private AudioSource audioSource;
    private GameObject owner;
    private float originalMaxSpeed;
    private float originalDrag;

    void Start()
    {
        owner = gameObject;
        ufoController = GetComponent<UFOController>();
        rb = GetComponent<Rigidbody>();

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.loop = false;

        // Create force field visual if not assigned
        if (forceFieldVisual == null)
        {
            CreateForceField();
        }

        // Hide force field initially
        if (forceFieldVisual != null)
        {
            forceFieldVisual.SetActive(false);
        }
    }

    void Update()
    {
        // Update dash
        if (isDashing)
        {
            // Update force field position
            UpdateForceFieldPosition();

            // Check if dash duration expired
            if (Time.time >= dashEndTime)
            {
                DeactivateDash();
            }
        }
    }

    void FixedUpdate()
    {
        // Apply forward thrust during dash
        if (isDashing && rb != null && ufoController != null)
        {
            // Get current velocity
            Vector3 currentVelocity = rb.velocity;
            Vector3 forwardDirection = transform.forward;

            // Project velocity onto forward direction
            float currentForwardSpeed = Vector3.Dot(currentVelocity, forwardDirection);

            // Calculate target forward speed
            float targetForwardSpeed = ufoController.maxSpeed * forwardSpeedMultiplier;

            // If current forward speed is below target, apply forward force
            if (currentForwardSpeed < targetForwardSpeed)
            {
                // Apply strong forward force to overcome drag and reach target speed
                float forceMagnitude = ufoController.acceleration * forwardSpeedMultiplier;
                rb.AddForce(forwardDirection * forceMagnitude, ForceMode.Acceleration);

                // Log every 0.5 seconds
                if (Time.time % 0.5f < 0.02f)
                {
                    Debug.Log($"[DASH] {gameObject.name} - Forward speed: {currentForwardSpeed:F1} / {targetForwardSpeed:F1} (target: {forwardSpeedMultiplier}x max speed)");
                    Debug.Log($"[DASH] Applying forward force: {forceMagnitude:F1}");
                }
            }
        }
    }

    void CreateForceField()
    {
        // Create a semi-transparent sphere in front of UFO
        forceFieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        forceFieldVisual.name = "DashForceField";
        forceFieldVisual.transform.SetParent(transform);
        forceFieldVisual.transform.localScale = Vector3.one * forceFieldSize;

        // Remove collider (visual only)
        Collider col = forceFieldVisual.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        // Create transparent material
        Material forceFieldMat = new Material(Shader.Find("Unlit/Transparent"));
        forceFieldMat.color = forceFieldColor;
        forceFieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        forceFieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        forceFieldMat.SetInt("_ZWrite", 0);
        forceFieldMat.renderQueue = 3000;

        Renderer renderer = forceFieldVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = forceFieldMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        forceFieldVisual.SetActive(false);
    }

    void UpdateForceFieldPosition()
    {
        if (forceFieldVisual != null)
        {
            // Position force field in front of UFO
            forceFieldVisual.transform.localPosition = Vector3.forward * forceFieldDistance;
        }
    }

    public bool TryFire()
    {
        // Can't dash if already dashing
        if (isDashing)
            return false;

        // Activate dash
        ActivateDash();
        return true;
    }

    void ActivateDash()
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;

        Debug.Log($"[DASH] {gameObject.name} activated dash for {dashDuration} seconds");

        // Store original values and temporarily boost max speed
        if (ufoController != null)
        {
            originalMaxSpeed = ufoController.maxSpeed;
            ufoController.maxSpeed = originalMaxSpeed * forwardSpeedMultiplier;
            Debug.Log($"[DASH] Max speed boosted: {originalMaxSpeed:F1} → {ufoController.maxSpeed:F1}");
        }

        // Reduce drag temporarily for faster acceleration
        if (rb != null)
        {
            originalDrag = rb.drag;
            rb.drag = originalDrag * 0.5f; // Half drag for better acceleration
            Debug.Log($"[DASH] Drag reduced: {originalDrag:F2} → {rb.drag:F2}");
        }

        // Show force field
        if (forceFieldVisual != null)
        {
            forceFieldVisual.SetActive(true);
        }

        // Play dash sound
        if (audioSource != null && dashSound != null)
        {
            audioSource.PlayOneShot(dashSound);
        }

        // Play looping dash sound
        if (audioSource != null && dashLoopSound != null)
        {
            audioSource.clip = dashLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void DeactivateDash()
    {
        isDashing = false;

        Debug.Log($"[DASH] {gameObject.name} dash ended");

        // Restore original max speed
        if (ufoController != null)
        {
            ufoController.maxSpeed = originalMaxSpeed;
            Debug.Log($"[DASH] Max speed restored to {originalMaxSpeed:F1}");
        }

        // Restore original drag
        if (rb != null)
        {
            rb.drag = originalDrag;
            Debug.Log($"[DASH] Drag restored to {originalDrag:F2}");
        }

        // Hide force field
        if (forceFieldVisual != null)
        {
            forceFieldVisual.SetActive(false);
        }

        // Stop looping sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Disable this weapon component (weapon used up)
        enabled = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only deal ramming damage while dashing
        if (!isDashing)
            return;

        // Check if we hit another UFO
        GameObject hitObject = collision.gameObject;
        Transform checkTransform = collision.transform;
        bool isPlayer = false;

        // Check the hit object and all parents for "Player" tag
        while (checkTransform != null)
        {
            if (checkTransform.CompareTag("Player"))
            {
                hitObject = checkTransform.gameObject;
                isPlayer = true;
                break;
            }
            checkTransform = checkTransform.parent;
        }

        // If we hit another UFO (not ourselves)
        if (isPlayer && hitObject != owner)
        {
            // Deal ramming damage
            UFOHealth health = hitObject.GetComponent<UFOHealth>();
            if (health != null)
            {
                health.TakeDamage(rammingDamage, owner);
                Debug.Log($"[DASH] {gameObject.name} rammed {hitObject.name} for {rammingDamage} damage!");
            }
        }
    }

    public bool IsActive()
    {
        return isDashing;
    }

    public bool CanFire()
    {
        return !isDashing;
    }
}
