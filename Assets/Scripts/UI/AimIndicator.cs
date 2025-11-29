using UnityEngine;

/// <summary>
/// Visual aim indicator that shows where the UFO's weapons are currently aimed
/// Displays a reticle in 3D space at the aim point
/// </summary>
public class AimIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UFO Controller to get aim direction from")]
    public UFOController ufoController;

    [Header("Visual Settings")]
    [Tooltip("Distance from UFO to place the reticle (units)")]
    public float reticleDistance = 50f;

    [Tooltip("Size of the reticle (scale multiplier)")]
    public float reticleSize = 1f;

    [Tooltip("Color of the reticle")]
    public Color reticleColor = new Color(0f, 1f, 0.5f, 0.8f); // Cyan-green

    [Tooltip("How quickly the reticle moves to new aim position")]
    public float smoothSpeed = 15f;

    [Header("Auto-Created Visuals")]
    [Tooltip("The reticle GameObject (auto-created if null)")]
    public GameObject reticleObject;

    [Header("Advanced Settings")]
    [Tooltip("Show reticle only when weapon is ready to fire")]
    public bool hideWhenCantFire = false;

    [Tooltip("Pulse speed (0 = no pulse)")]
    public float pulseSpeed = 2f;

    [Tooltip("Pulse amount (0-1)")]
    public float pulseAmount = 0.2f;

    private Material reticleMaterial;
    private float basePulse = 1f;
    private WeaponManager weaponManager;

    void Start()
    {
        // Get UFO controller if not assigned
        if (ufoController == null)
        {
            ufoController = GetComponent<UFOController>();
            if (ufoController == null)
            {
                Debug.LogError("AimIndicator: No UFOController found! Assign manually or attach to UFO with UFOController.");
                enabled = false;
                return;
            }
        }

        // Get weapon manager for fire-ready detection
        if (hideWhenCantFire)
        {
            weaponManager = GetComponent<WeaponManager>();
        }

        // Create reticle if not assigned
        if (reticleObject == null)
        {
            CreateReticle();
        }
    }

    void CreateReticle()
    {
        // Create parent object for reticle
        reticleObject = new GameObject("AimReticle");
        reticleObject.transform.SetParent(transform); // Parent to UFO for scene organization
        reticleObject.transform.localPosition = Vector3.zero;

        // Create outer ring
        GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outerRing.name = "OuterRing";
        outerRing.transform.SetParent(reticleObject.transform);
        outerRing.transform.localPosition = Vector3.zero;
        outerRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Rotate to face forward
        outerRing.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f); // Thin ring

        // Remove collider (we don't want physics on this)
        Destroy(outerRing.GetComponent<Collider>());

        // Create inner dot
        GameObject innerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        innerDot.name = "InnerDot";
        innerDot.transform.SetParent(reticleObject.transform);
        innerDot.transform.localPosition = Vector3.zero;
        innerDot.transform.localScale = Vector3.one * 0.3f;

        // Remove collider
        Destroy(innerDot.GetComponent<Collider>());

        // Create four tick marks around the ring
        for (int i = 0; i < 4; i++)
        {
            GameObject tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tick.name = $"Tick_{i}";
            tick.transform.SetParent(reticleObject.transform);

            // Position ticks at cardinal directions
            float angle = i * 90f * Mathf.Deg2Rad;
            float distance = 1.2f;
            tick.transform.localPosition = new Vector3(
                Mathf.Sin(angle) * distance,
                0f,
                Mathf.Cos(angle) * distance
            );

            // Make ticks small rectangles
            tick.transform.localScale = new Vector3(0.1f, 0.1f, 0.4f);
            tick.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);

            // Remove collider
            Destroy(tick.GetComponent<Collider>());
        }

        // Create unlit material for all reticle parts
        reticleMaterial = new Material(Shader.Find("Unlit/Color"));
        reticleMaterial.color = reticleColor;

        // Apply material to all children
        foreach (Renderer renderer in reticleObject.GetComponentsInChildren<Renderer>())
        {
            renderer.material = reticleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Set initial scale
        reticleObject.transform.localScale = Vector3.one * reticleSize;
    }

    void Update()
    {
        if (reticleObject == null || ufoController == null)
            return;

        // Check if we should hide the reticle
        if (hideWhenCantFire && weaponManager != null)
        {
            bool canFire = weaponManager.CanCurrentWeaponFire();
            reticleObject.SetActive(canFire);

            if (!canFire)
                return;
        }

        // Use UFO's transform.forward directly - matches camera exactly
        // No velocity-based calculations - reticle locked to screen center
        Vector3 targetPosition = transform.position + (transform.forward * reticleDistance);

        // INSTANT position - no smoothing, reticle locked to aim direction
        reticleObject.transform.position = targetPosition;

        // Face the reticle toward the UFO (so it's always visible)
        reticleObject.transform.rotation = Quaternion.LookRotation(
            (transform.position - reticleObject.transform.position).normalized
        );

        // Apply pulse effect
        if (pulseSpeed > 0f && pulseAmount > 0f)
        {
            basePulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            reticleObject.transform.localScale = Vector3.one * reticleSize * basePulse;
        }
    }

    /// <summary>
    /// Update reticle color (useful for indicating different weapon types)
    /// </summary>
    public void SetReticleColor(Color color)
    {
        reticleColor = color;
        if (reticleMaterial != null)
        {
            reticleMaterial.color = color;
        }
    }

    /// <summary>
    /// Update reticle distance (useful for indicating weapon range)
    /// </summary>
    public void SetReticleDistance(float distance)
    {
        reticleDistance = distance;
    }

    void OnDestroy()
    {
        // Clean up created reticle
        if (reticleObject != null)
        {
            Destroy(reticleObject);
        }

        // Clean up material
        if (reticleMaterial != null)
        {
            Destroy(reticleMaterial);
        }
    }
}
