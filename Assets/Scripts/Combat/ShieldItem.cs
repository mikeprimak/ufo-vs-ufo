using UnityEngine;

/// <summary>
/// Shield defensive item - creates a temporary invincibility bubble around the UFO
/// Single use item that activates on button press
/// </summary>
public class ShieldItem : MonoBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("How long the shield lasts (seconds)")]
    public float shieldDuration = 5f;

    [Tooltip("Size of the shield bubble")]
    public float shieldSize = 10f;

    [Tooltip("Shield bubble color")]
    public Color shieldColor = new Color(0f, 0.8f, 1f, 0.15f); // Cyan, more transparent

    [Header("Visual Settings")]
    [Tooltip("Pulse speed for breathing effect")]
    public float pulseSpeed = 2f;

    [Tooltip("Pulse intensity (0-1)")]
    public float pulseAmount = 0.15f;

    [Header("Audio")]
    [Tooltip("Sound to play when shield activates")]
    public AudioClip activateSound;

    [Tooltip("Sound to play when shield deactivates")]
    public AudioClip deactivateSound;

    // Shield state
    private bool isActive = false;
    private float shieldEndTime = 0f;
    private GameObject shieldVisual;
    private UFOHealth ufoHealth;

    // Track original invincibility state
    private bool wasInvincibleBefore = false;

    void Start()
    {
        ufoHealth = GetComponent<UFOHealth>();
    }

    void Update()
    {
        if (!isActive)
            return;

        // Update shield visual (pulse effect)
        if (shieldVisual != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            shieldVisual.transform.localScale = Vector3.one * shieldSize * pulse;
        }

        // Check if shield expired
        if (Time.time >= shieldEndTime)
        {
            DeactivateShield();
        }
    }

    /// <summary>
    /// Check if shield can be activated
    /// </summary>
    public bool CanActivate()
    {
        return !isActive && enabled;
    }

    /// <summary>
    /// Try to activate the shield
    /// </summary>
    public bool TryActivate()
    {
        if (!CanActivate())
            return false;

        ActivateShield();
        return true;
    }

    /// <summary>
    /// Activate the shield bubble
    /// </summary>
    void ActivateShield()
    {
        isActive = true;
        shieldEndTime = Time.time + shieldDuration;

        // Store current invincibility state and make UFO invincible
        if (ufoHealth != null)
        {
            wasInvincibleBefore = ufoHealth.IsInvincible();
            // Use reflection or add a public method to set invincibility
            // For now, we'll track it separately and block damage in a different way
        }

        // Create shield visual
        CreateShieldVisual();

        // Play activation sound
        if (activateSound != null)
        {
            AudioSource.PlayClipAtPoint(activateSound, transform.position, 1f);
        }
    }

    /// <summary>
    /// Deactivate the shield
    /// </summary>
    void DeactivateShield()
    {
        isActive = false;

        // Destroy shield visual
        if (shieldVisual != null)
        {
            Destroy(shieldVisual);
            shieldVisual = null;
        }

        // Play deactivation sound
        if (deactivateSound != null)
        {
            AudioSource.PlayClipAtPoint(deactivateSound, transform.position, 1f);
        }
    }

    /// <summary>
    /// Create the shield bubble visual
    /// </summary>
    void CreateShieldVisual()
    {
        // Create sphere primitive
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.name = "ShieldBubble";
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = Vector3.one * shieldSize;

        // Remove collider (visual only)
        Collider col = shieldVisual.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        // Set up transparent material for URP
        Renderer renderer = shieldVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Try URP Unlit shader first (works in Universal Render Pipeline)
            Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (urpShader != null)
            {
                Material shieldMat = new Material(urpShader);
                shieldMat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                shieldMat.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
                shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                shieldMat.SetInt("_ZWrite", 0);
                shieldMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                shieldMat.renderQueue = 3000;
                shieldMat.SetColor("_BaseColor", shieldColor);
                renderer.material = shieldMat;
            }
            else
            {
                // Fallback: try Sprites/Default which supports transparency
                Shader fallbackShader = Shader.Find("Sprites/Default");
                if (fallbackShader != null)
                {
                    Material shieldMat = new Material(fallbackShader);
                    shieldMat.color = shieldColor;
                    renderer.material = shieldMat;
                }
                else
                {
                    // Last resort: just set color on default material
                    renderer.material.color = shieldColor;
                }
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    /// <summary>
    /// Check if shield is currently active (for damage blocking)
    /// </summary>
    public bool IsShieldActive()
    {
        return isActive;
    }

    /// <summary>
    /// Get remaining shield time
    /// </summary>
    public float GetRemainingTime()
    {
        if (!isActive)
            return 0f;
        return Mathf.Max(0f, shieldEndTime - Time.time);
    }

    /// <summary>
    /// Get shield duration for UI display
    /// </summary>
    public float GetShieldDuration()
    {
        return shieldDuration;
    }

    void OnDisable()
    {
        // Clean up shield if disabled while active
        if (isActive)
        {
            DeactivateShield();
        }
    }

    void OnDestroy()
    {
        // Clean up shield visual
        if (shieldVisual != null)
        {
            Destroy(shieldVisual);
        }
    }
}
