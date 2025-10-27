using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Circular rotating minimap that shows UFO positions relative to player
/// Shows UFOs within range as blips, and shows edge indicators for distant UFOs
/// Minimap rotates with player so "up" is always forward
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player UFO to center the minimap on")]
    public Transform playerUFO;

    [Tooltip("Container for minimap blips (will rotate with player direction)")]
    public RectTransform blipContainer;

    [Tooltip("Prefab for UFO blip (small colored circle)")]
    public GameObject blipPrefab;

    [Header("Minimap Settings")]
    [Tooltip("Detection range in world units (UFOs beyond this show on edge)")]
    public float detectionRange = 100f;

    [Tooltip("Radius of minimap in pixels")]
    public float minimapRadius = 75f;

    [Tooltip("Scale factor: world units to minimap pixels")]
    public float worldToMinimapScale = 0.5f;

    [Tooltip("Size of UFO blips in pixels")]
    public float blipSize = 8f;

    [Tooltip("Color for player blip (usually brighter/different)")]
    public Color playerBlipColor = Color.cyan;

    [Tooltip("How often to update minimap (seconds)")]
    public float updateInterval = 0.1f;

    private Dictionary<GameObject, GameObject> blipCache = new Dictionary<GameObject, GameObject>();
    private float lastUpdateTime = 0f;

    void Update()
    {
        // Update at fixed intervals for performance
        if (Time.time - lastUpdateTime < updateInterval)
            return;

        lastUpdateTime = Time.time;

        if (playerUFO == null)
        {
            // Try to find player UFO
            playerUFO = FindPlayerUFO();
            if (playerUFO == null)
                return;
        }

        UpdateMinimap();
    }

    /// <summary>
    /// Find the player-controlled UFO
    /// </summary>
    Transform FindPlayerUFO()
    {
        GameObject[] ufos = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject ufo in ufos)
        {
            PlayerStats stats = ufo.GetComponent<PlayerStats>();
            if (stats != null && stats.isHumanPlayer)
            {
                return ufo.transform;
            }
        }
        return null;
    }

    /// <summary>
    /// Update all UFO positions on minimap
    /// </summary>
    void UpdateMinimap()
    {
        if (blipContainer == null)
            return;

        // Rotate minimap container to align with player's forward direction
        // Negative because we want "up" on minimap to be player's forward
        Vector3 playerForward = playerUFO.forward;
        float playerAngle = Mathf.Atan2(playerForward.x, playerForward.z) * Mathf.Rad2Deg;
        blipContainer.localRotation = Quaternion.Euler(0, 0, -playerAngle);

        // Find all UFOs
        GameObject[] allUFOs = GameObject.FindGameObjectsWithTag("Player");
        HashSet<GameObject> activeUFOs = new HashSet<GameObject>(allUFOs);

        // Update blips for all UFOs
        foreach (GameObject ufo in allUFOs)
        {
            if (ufo == null)
                continue;

            // Check if UFO is dead
            UFOHealth health = ufo.GetComponent<UFOHealth>();
            if (health != null && health.IsDead())
                continue; // Don't show dead UFOs

            UpdateBlipForUFO(ufo);
        }

        // Remove blips for UFOs that no longer exist or are dead
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in blipCache)
        {
            if (kvp.Key == null || !activeUFOs.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            else
            {
                // Check if dead
                UFOHealth health = kvp.Key.GetComponent<UFOHealth>();
                if (health != null && health.IsDead())
                {
                    toRemove.Add(kvp.Key);
                    if (kvp.Value != null)
                        Destroy(kvp.Value);
                }
            }
        }

        foreach (GameObject key in toRemove)
        {
            blipCache.Remove(key);
        }
    }

    /// <summary>
    /// Update or create blip for a specific UFO
    /// </summary>
    void UpdateBlipForUFO(GameObject ufo)
    {
        // Get or create blip
        GameObject blip;
        if (!blipCache.ContainsKey(ufo))
        {
            // Create new blip
            blip = Instantiate(blipPrefab, blipContainer);
            blipCache[ufo] = blip;

            // Set blip size
            RectTransform blipRect = blip.GetComponent<RectTransform>();
            if (blipRect != null)
            {
                blipRect.sizeDelta = new Vector2(blipSize, blipSize);
            }

            // Set blip color based on UFO color
            Image blipImage = blip.GetComponent<Image>();
            if (blipImage != null)
            {
                bool isPlayer = (ufo == playerUFO.gameObject);
                if (isPlayer)
                {
                    blipImage.color = playerBlipColor;
                }
                else
                {
                    // Get UFO color from UFOColorIdentity component
                    UFOColorIdentity colorIdentity = ufo.GetComponent<UFOColorIdentity>();
                    if (colorIdentity != null)
                    {
                        blipImage.color = colorIdentity.GetDisplayColor();
                    }
                    else
                    {
                        blipImage.color = Color.white; // Fallback
                    }
                }
            }
        }
        else
        {
            blip = blipCache[ufo];
        }

        if (blip == null)
            return;

        // Calculate relative position (in player's local space)
        Vector3 relativePos = ufo.transform.position - playerUFO.position;

        // Convert to 2D position (ignore Y, use X and Z)
        Vector2 minimapPos = new Vector2(relativePos.x, relativePos.z);

        // Scale to minimap size
        minimapPos *= worldToMinimapScale;

        // Check if outside detection range
        float distance = minimapPos.magnitude;
        bool isOutsideRange = distance > (detectionRange * worldToMinimapScale);

        if (isOutsideRange)
        {
            // Clamp to edge of minimap
            minimapPos = minimapPos.normalized * minimapRadius;
        }

        // Apply position (in blipContainer's local space)
        RectTransform blipRectTransform = blip.GetComponent<RectTransform>();
        if (blipRectTransform != null)
        {
            blipRectTransform.anchoredPosition = minimapPos;
        }

        // Optional: Make edge blips smaller or different style
        if (isOutsideRange)
        {
            // Could make them smaller or change opacity
            Image blipImage = blip.GetComponent<Image>();
            if (blipImage != null)
            {
                Color color = blipImage.color;
                color.a = 0.6f; // Slightly transparent for edge blips
                blipImage.color = color;
            }
        }
        else
        {
            // Full opacity for in-range blips
            Image blipImage = blip.GetComponent<Image>();
            if (blipImage != null)
            {
                Color color = blipImage.color;
                color.a = 1f;
                blipImage.color = color;
            }
        }
    }

    /// <summary>
    /// Clean up all blips
    /// </summary>
    void OnDestroy()
    {
        foreach (var kvp in blipCache)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        blipCache.Clear();
    }
}
