using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays combat events (hits, kills) in the top-left corner
/// Shows color-coded messages like "Red Hit Green!" and "Blue Killed Yellow!"
/// Messages fade out after a few seconds to keep the feed clean
/// </summary>
public class CombatLogUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent container for combat log messages")]
    public Transform logContainer;

    [Tooltip("Prefab for a single log entry (TextMeshPro text)")]
    public GameObject logEntryPrefab;

    [Header("Settings")]
    [Tooltip("How long messages stay visible (seconds)")]
    public float messageDuration = 4f;

    [Tooltip("Maximum number of messages shown at once")]
    public int maxMessages = 5;

    [Tooltip("Font size for log entries")]
    public float fontSize = 18f;

    [Header("Player Colors")]
    [Tooltip("Color for player UFO (usually cyan)")]
    public Color playerColor = new Color(0f, 1f, 1f); // Cyan

    [Tooltip("Color for AI UFOs (usually orange)")]
    public Color aiColor = new Color(1f, 0.6f, 0f); // Orange

    private List<LogEntry> activeEntries = new List<LogEntry>();

    private class LogEntry
    {
        public GameObject gameObject;
        public TextMeshProUGUI text;
        public float spawnTime;
    }

    private static CombatLogUI instance;

    void Awake()
    {
        // Singleton pattern for easy access
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("[COMBAT LOG] Multiple CombatLogUI instances found! Using first one.");
        }
    }

    void Update()
    {
        // Remove old messages
        for (int i = activeEntries.Count - 1; i >= 0; i--)
        {
            LogEntry entry = activeEntries[i];
            float age = Time.time - entry.spawnTime;

            if (age > messageDuration)
            {
                // Destroy and remove
                Destroy(entry.gameObject);
                activeEntries.RemoveAt(i);
            }
            else if (age > messageDuration - 1f)
            {
                // Fade out in last second
                float alpha = messageDuration - age;
                Color color = entry.text.color;
                color.a = alpha;
                entry.text.color = color;
            }
        }
    }

    /// <summary>
    /// Add a combat event to the log
    /// </summary>
    void AddLogEntry(string message, Color color)
    {
        // Remove oldest entry if at max capacity
        if (activeEntries.Count >= maxMessages)
        {
            LogEntry oldest = activeEntries[0];
            Destroy(oldest.gameObject);
            activeEntries.RemoveAt(0);
        }

        // Create new entry
        GameObject entryObj = Instantiate(logEntryPrefab, logContainer);
        TextMeshProUGUI textComponent = entryObj.GetComponent<TextMeshProUGUI>();

        if (textComponent == null)
        {
            Debug.LogError("[COMBAT LOG] Log entry prefab missing TextMeshProUGUI component!");
            Destroy(entryObj);
            return;
        }

        textComponent.text = message;
        textComponent.fontSize = fontSize;
        textComponent.color = color;

        LogEntry entry = new LogEntry
        {
            gameObject = entryObj,
            text = textComponent,
            spawnTime = Time.time
        };

        activeEntries.Add(entry);
    }

    /// <summary>
    /// Get player's display color based on UFO material
    /// </summary>
    Color GetPlayerColor(GameObject player)
    {
        if (player == null) return Color.white;

        // Try to get color from material (works for all UFOs)
        Renderer renderer = player.GetComponentInChildren<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // Get base color from material (URP uses _BaseColor)
            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                Color color = renderer.sharedMaterial.GetColor("_BaseColor");
                // Return RGB only (ignore alpha)
                return new Color(color.r, color.g, color.b, 1f);
            }
            // Legacy/Built-in render pipeline uses _Color
            else if (renderer.sharedMaterial.HasProperty("_Color"))
            {
                Color color = renderer.sharedMaterial.color;
                return new Color(color.r, color.g, color.b, 1f);
            }
        }

        // Fallback: try multiple renderers (in case UFO_Visual has the material)
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r != null && r.sharedMaterial != null)
            {
                if (r.sharedMaterial.HasProperty("_BaseColor"))
                {
                    Color color = r.sharedMaterial.GetColor("_BaseColor");
                    // Skip transparent/black materials
                    if (color.r > 0.1f || color.g > 0.1f || color.b > 0.1f)
                    {
                        return new Color(color.r, color.g, color.b, 1f);
                    }
                }
            }
        }

        // Default fallback
        return Color.white;
    }

    /// <summary>
    /// Get player's display name with color formatting
    /// Uses the actual color name (Red, Blue, Green, Yellow, etc.)
    /// </summary>
    string GetColoredPlayerName(GameObject player)
    {
        if (player == null) return "<color=white>Unknown</color>";

        Color color = GetPlayerColor(player);

        // Determine color name from the color
        string colorName = GetColorName(color);

        // Convert color to hex for TextMeshPro rich text
        string colorHex = ColorUtility.ToHtmlStringRGB(color);

        return $"<color=#{colorHex}>{colorName}</color>";
    }

    /// <summary>
    /// Convert a color to its common name (Red, Blue, Green, Yellow, etc.)
    /// </summary>
    string GetColorName(Color color)
    {
        // Define color ranges for common colors
        float r = color.r;
        float g = color.g;
        float b = color.b;

        // Red - high R, low G and B
        if (r > 0.7f && g < 0.3f && b < 0.3f)
            return "Red";

        // Green - high G, low R and B
        if (g > 0.7f && r < 0.3f && b < 0.3f)
            return "Green";

        // Blue - high B, low R and G
        if (b > 0.7f && r < 0.3f && g < 0.3f)
            return "Blue";

        // Yellow - high R and G, low B
        if (r > 0.7f && g > 0.7f && b < 0.3f)
            return "Yellow";

        // Cyan - high G and B, low R
        if (r < 0.3f && g > 0.7f && b > 0.7f)
            return "Cyan";

        // Magenta/Pink - high R and B, low G
        if (r > 0.7f && g < 0.3f && b > 0.7f)
            return "Magenta";

        // Orange - high R, medium G, low B
        if (r > 0.7f && g > 0.3f && g < 0.7f && b < 0.3f)
            return "Orange";

        // Purple - medium R, low G, high B
        if (r > 0.3f && r < 0.7f && g < 0.3f && b > 0.7f)
            return "Purple";

        // White - all high
        if (r > 0.8f && g > 0.8f && b > 0.8f)
            return "White";

        // Black/Gray - all low/medium
        if (r < 0.3f && g < 0.3f && b < 0.3f)
            return "Black";

        // Gray - all medium
        if (Mathf.Abs(r - g) < 0.2f && Mathf.Abs(g - b) < 0.2f && Mathf.Abs(r - b) < 0.2f)
            return "Gray";

        // Default fallback
        return "Unknown";
    }

    // ===== PUBLIC STATIC METHODS FOR LOGGING EVENTS =====

    /// <summary>
    /// Log a hit event (attacker damaged victim)
    /// </summary>
    public static void LogHit(GameObject attacker, GameObject victim, int damage)
    {
        if (instance == null) return;

        string attackerName = instance.GetColoredPlayerName(attacker);
        string victimName = instance.GetColoredPlayerName(victim);

        string message = $"{attackerName} hit {victimName}!";
        instance.AddLogEntry(message, Color.white);
    }

    /// <summary>
    /// Log a kill event (killer eliminated victim)
    /// </summary>
    public static void LogKill(GameObject killer, GameObject victim)
    {
        if (instance == null) return;

        string killerName = instance.GetColoredPlayerName(killer);
        string victimName = instance.GetColoredPlayerName(victim);

        string message = $"{killerName} killed {victimName}!";
        instance.AddLogEntry(message, new Color(1f, 1f, 0.5f)); // Yellowish for kills
    }

    /// <summary>
    /// Log a suicide/environmental death
    /// </summary>
    public static void LogSuicide(GameObject victim)
    {
        if (instance == null) return;

        string victimName = instance.GetColoredPlayerName(victim);

        string message = $"{victimName} self-destructed!";
        instance.AddLogEntry(message, new Color(0.7f, 0.7f, 0.7f)); // Gray for suicide
    }

    /// <summary>
    /// Log a weapon pickup
    /// </summary>
    public static void LogWeaponPickup(GameObject player, string weaponName)
    {
        if (instance == null) return;

        string playerName = instance.GetColoredPlayerName(player);

        string message = $"{playerName} picked up {weaponName}";
        instance.AddLogEntry(message, new Color(0.8f, 0.8f, 0.8f)); // Light gray for pickups
    }
}
