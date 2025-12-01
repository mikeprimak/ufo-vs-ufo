using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Invisibility defensive item - makes UFO invisible to players and homing weapons
/// UFO can still take damage but cannot be seen or tracked
/// Single use item with duration
/// </summary>
public class InvisibilityItem : MonoBehaviour
{
    [Header("Invisibility Settings")]
    [Tooltip("How long invisibility lasts (seconds)")]
    public float invisibilityDuration = 5f;

    [Tooltip("How transparent the UFO becomes (0 = fully invisible, 1 = fully visible)")]
    [Range(0f, 1f)]
    public float invisibilityAlpha = 0.1f;

    [Header("Audio")]
    [Tooltip("Sound to play when invisibility activates")]
    public AudioClip activateSound;

    [Tooltip("Sound to play when invisibility deactivates")]
    public AudioClip deactivateSound;

    // Invisibility state
    private bool isActive = false;
    private float invisibilityEndTime = 0f;

    // Store original renderer states to restore later
    private List<RendererState> originalRendererStates = new List<RendererState>();

    private struct RendererState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public bool wasEnabled;
    }

    void Update()
    {
        if (!isActive)
            return;

        // Check if invisibility expired
        if (Time.time >= invisibilityEndTime)
        {
            DeactivateInvisibility();
        }
    }

    /// <summary>
    /// Check if invisibility can be activated
    /// </summary>
    public bool CanActivate()
    {
        return !isActive && enabled;
    }

    /// <summary>
    /// Try to activate invisibility
    /// </summary>
    public bool TryActivate()
    {
        if (!CanActivate())
            return false;

        ActivateInvisibility();
        return true;
    }

    /// <summary>
    /// Activate invisibility
    /// </summary>
    void ActivateInvisibility()
    {
        isActive = true;
        invisibilityEndTime = Time.time + invisibilityDuration;

        Debug.Log($"[INVISIBILITY] {gameObject.name} activated invisibility for {invisibilityDuration} seconds");

        // Store and modify all renderers
        originalRendererStates.Clear();
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in allRenderers)
        {
            // Store original state
            RendererState state = new RendererState
            {
                renderer = rend,
                originalMaterials = rend.materials,
                wasEnabled = rend.enabled
            };
            originalRendererStates.Add(state);

            // Make semi-transparent
            Material[] newMaterials = new Material[rend.materials.Length];
            for (int i = 0; i < rend.materials.Length; i++)
            {
                // Create transparent version of material
                newMaterials[i] = CreateTransparentMaterial(rend.materials[i]);
            }
            rend.materials = newMaterials;
        }

        // Play activation sound
        if (activateSound != null)
        {
            AudioSource.PlayClipAtPoint(activateSound, transform.position, 1f);
        }
    }

    /// <summary>
    /// Create a transparent version of a material
    /// </summary>
    Material CreateTransparentMaterial(Material original)
    {
        // Try URP shader for transparency
        Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (urpShader != null)
        {
            Material mat = new Material(urpShader);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            // Get original color and make it very transparent
            Color originalColor = Color.white;
            if (original.HasProperty("_BaseColor"))
                originalColor = original.GetColor("_BaseColor");
            else if (original.HasProperty("_Color"))
                originalColor = original.GetColor("_Color");

            originalColor.a = invisibilityAlpha;
            mat.SetColor("_BaseColor", originalColor);

            return mat;
        }
        else
        {
            // Fallback: just modify alpha on copy
            Material mat = new Material(original);
            Color c = mat.color;
            c.a = invisibilityAlpha;
            mat.color = c;
            return mat;
        }
    }

    /// <summary>
    /// Deactivate invisibility and restore original appearance
    /// </summary>
    void DeactivateInvisibility()
    {
        isActive = false;

        Debug.Log($"[INVISIBILITY] {gameObject.name} invisibility ended");

        // Restore original materials
        foreach (RendererState state in originalRendererStates)
        {
            if (state.renderer != null)
            {
                state.renderer.materials = state.originalMaterials;
                state.renderer.enabled = state.wasEnabled;
            }
        }
        originalRendererStates.Clear();

        // Play deactivation sound
        if (deactivateSound != null)
        {
            AudioSource.PlayClipAtPoint(deactivateSound, transform.position, 1f);
        }
    }

    /// <summary>
    /// Check if invisibility is currently active (for homing missile checks)
    /// </summary>
    public bool IsInvisible()
    {
        return isActive;
    }

    /// <summary>
    /// Get remaining invisibility time
    /// </summary>
    public float GetRemainingTime()
    {
        if (!isActive)
            return 0f;
        return Mathf.Max(0f, invisibilityEndTime - Time.time);
    }

    void OnDisable()
    {
        // Clean up if disabled while active
        if (isActive)
        {
            DeactivateInvisibility();
        }
    }
}
