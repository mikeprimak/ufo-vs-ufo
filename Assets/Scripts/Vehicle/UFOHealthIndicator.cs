using UnityEngine;

/// <summary>
/// Visual health indicator - displays floating orbs above UFO that show remaining HP
/// Orbs change color based on health: 3 HP = Green, 2 HP = Yellow, 1 HP = Red
/// </summary>
public class UFOHealthIndicator : MonoBehaviour
{
    [Header("Orb Settings")]
    [Tooltip("Prefab for health orb (sphere)")]
    public GameObject orbPrefab;

    [Tooltip("Height above UFO center")]
    public float height = 8f;

    [Tooltip("Radius of orbit circle")]
    public float orbitRadius = 6f;

    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 60f;

    [Tooltip("Orb size")]
    public float orbScale = 0.8f;

    [Header("Colors")]
    [Tooltip("Color when at 3 HP")]
    public Color fullHealthColor = Color.green;

    [Tooltip("Color when at 2 HP")]
    public Color mediumHealthColor = Color.yellow;

    [Tooltip("Color when at 1 HP")]
    public Color lowHealthColor = Color.red;

    private GameObject[] orbs = new GameObject[3];
    private UFOHealth ufoHealth;
    private int lastKnownHealth = 3;

    void Start()
    {
        ufoHealth = GetComponent<UFOHealth>();

        if (ufoHealth == null)
        {
            Debug.LogError("UFOHealthIndicator: No UFOHealth component found!");
            enabled = false;
            return;
        }

        Debug.Log($"[HEALTH INDICATOR] Starting for {gameObject.name}");

        // Create orbs if no prefab assigned, use procedural spheres
        if (orbPrefab == null)
        {
            Debug.Log($"[HEALTH INDICATOR] Creating procedural orbs for {gameObject.name}");
            CreateProceduralOrbs();
        }
        else
        {
            Debug.Log($"[HEALTH INDICATOR] Creating orbs from prefab for {gameObject.name}");
            CreateOrbsFromPrefab();
        }

        // Set initial health tracking
        lastKnownHealth = ufoHealth.GetCurrentHealth();

        // Initial health display - MUST activate orbs and set color
        UpdateOrbDisplay();

        Debug.Log($"[HEALTH INDICATOR] Setup complete for {gameObject.name}, health: {ufoHealth.GetCurrentHealth()}");
    }

    void CreateProceduralOrbs()
    {
        for (int i = 0; i < 3; i++)
        {
            // Create sphere
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = $"HealthOrb_{i}";
            orb.transform.parent = transform;
            orb.transform.localScale = Vector3.one * orbScale;

            Debug.Log($"[HEALTH INDICATOR] Created orb {i} for {gameObject.name}, scale: {orbScale}");

            // Remove collider (visual only)
            Collider orbCollider = orb.GetComponent<Collider>();
            if (orbCollider != null)
                Destroy(orbCollider);

            // Create emissive material
            Renderer renderer = orb.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Use URP Lit shader for proper rendering
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                // Enable transparency
                mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                mat.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000; // Transparent render queue
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);

                // Set color with transparency (alpha = 0.6 for 60% opacity)
                Color transparentColor = new Color(fullHealthColor.r, fullHealthColor.g, fullHealthColor.b, 0.6f);
                mat.SetColor("_BaseColor", transparentColor);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", fullHealthColor * 2f); // Bright emission
                renderer.material = mat;
                Debug.Log($"[HEALTH INDICATOR] Set material for orb {i}, color: {fullHealthColor}");
            }

            orbs[i] = orb;
        }
    }

    void CreateOrbsFromPrefab()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject orb = Instantiate(orbPrefab, transform);
            orb.name = $"HealthOrb_{i}";
            orb.transform.localScale = Vector3.one * orbScale;
            orbs[i] = orb;
        }
    }

    void Update()
    {
        // Check if health changed
        int currentHealth = ufoHealth.GetCurrentHealth();
        if (currentHealth != lastKnownHealth)
        {
            UpdateOrbDisplay();
            lastKnownHealth = currentHealth;
        }

        // Rotate orbs around UFO
        PositionOrbs();
    }

    void PositionOrbs()
    {
        float angleStep = 360f / 3f; // 120 degrees between each orb
        float currentAngle = Time.time * rotationSpeed;

        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] != null && orbs[i].activeSelf)
            {
                float angle = currentAngle + (angleStep * i);
                float radians = angle * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(
                    Mathf.Cos(radians) * orbitRadius,
                    height,
                    Mathf.Sin(radians) * orbitRadius
                );

                orbs[i].transform.localPosition = offset;
            }
        }
    }

    void UpdateOrbDisplay()
    {
        int currentHealth = ufoHealth.GetCurrentHealth();

        // Show/hide orbs based on health
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] != null)
            {
                orbs[i].SetActive(i < currentHealth);
            }
        }

        // Update color based on health
        Color targetColor = fullHealthColor;
        if (currentHealth == 2)
            targetColor = mediumHealthColor;
        else if (currentHealth == 1)
            targetColor = lowHealthColor;

        // Apply color to all active orbs
        foreach (GameObject orb in orbs)
        {
            if (orb != null && orb.activeSelf)
            {
                Renderer renderer = orb.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Apply color with transparency (alpha = 0.6)
                    Color transparentColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.6f);
                    renderer.material.SetColor("_BaseColor", transparentColor);
                    renderer.material.SetColor("_EmissionColor", targetColor * 2f);
                }
            }
        }
    }

    void OnDestroy()
    {
        // Clean up orbs when UFO is destroyed
        foreach (GameObject orb in orbs)
        {
            if (orb != null)
                Destroy(orb);
        }
    }
}
