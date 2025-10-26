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

        // Get all renderers and find the most saturated (colorful) material
        // This ensures we get the UFO body color, not the gray dome
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        Color mostSaturatedColor = Color.white;
        float highestSaturation = 0f;

        foreach (Renderer r in renderers)
        {
            if (r != null && r.sharedMaterial != null)
            {
                Color color = Color.white;

                // Get base color from material (URP uses _BaseColor)
                if (r.sharedMaterial.HasProperty("_BaseColor"))
                {
                    color = r.sharedMaterial.GetColor("_BaseColor");
                }
                // Legacy/Built-in render pipeline uses _Color
                else if (r.sharedMaterial.HasProperty("_Color"))
                {
                    color = r.sharedMaterial.color;
                }
                else
                {
                    continue; // Skip if no color property
                }

                // Calculate color saturation (how colorful vs gray it is)
                // Saturation = max(r,g,b) - min(r,g,b)
                float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                float saturation = max - min;

                // Prefer more saturated (colorful) materials
                // This will pick Red/Green/Blue/Yellow over gray dome
                if (saturation > highestSaturation)
                {
                    highestSaturation = saturation;
                    mostSaturatedColor = new Color(color.r, color.g, color.b, 1f);
                }
            }
        }

        return mostSaturatedColor;
    }

    /// <summary>
    /// Get player's display name with color formatting
    /// Uses the actual color name (Red, Blue, Green, Yellow, etc.)
    /// </summary>
    string GetColoredPlayerName(GameObject player)
    {
        if (player == null) return "<color=white>Unknown</color>";

        // First, check if UFO has explicit color identity component (most reliable)
        UFOColorIdentity colorIdentity = player.GetComponent<UFOColorIdentity>();
        if (colorIdentity != null)
        {
            string colorName = colorIdentity.GetColorName();
            Color color = colorIdentity.GetDisplayColor();
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{colorHex}>{colorName}</color>";
        }

        // Fallback: try to detect color from material
        Color detectedColor = GetPlayerColor(player);
        string detectedColorName = GetColorName(detectedColor);
        string detectedColorHex = ColorUtility.ToHtmlStringRGB(detectedColor);

        return $"<color=#{detectedColorHex}>{detectedColorName}</color>";
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

        // Find which component is dominant
        float max = Mathf.Max(r, Mathf.Max(g, b));
        float min = Mathf.Min(r, Mathf.Min(g, b));
        float saturation = max - min;

        // Gray/White/Black - low saturation (all components similar)
        if (saturation < 0.2f)
        {
            if (max > 0.8f)
                return "White";
            else if (max < 0.3f)
                return "Black";
            else
                return "Gray";
        }

        // Determine color based on which component(s) are dominant
        // Yellow - BOTH R and G are high and close together, B is low
        if (r > 0.7f && g > 0.7f && b < 0.3f)
            return "Yellow";

        // Cyan - BOTH G and B are high, R is low
        if (r < 0.3f && g > 0.6f && b > 0.6f)
            return "Cyan";

        // Magenta - BOTH R and B are high, G is low
        if (r > 0.7f && g < 0.3f && b > 0.7f)
            return "Magenta";

        // Orange - R is highest, G is medium, B is low
        if (r > 0.7f && g > 0.3f && g < 0.8f && b < 0.3f)
            return "Orange";

        // Purple - B is high, R is medium, G is low
        if (r > 0.3f && r < 0.7f && g < 0.4f && b > 0.7f)
            return "Purple";

        // Primary colors - check which single component is dominant
        if (r > g && r > b)
        {
            // Red is dominant
            if (g < 0.3f && b < 0.3f)
                return "Red";
            else
                return "Orange"; // Fallback for reddish colors
        }
        else if (g > r && g > b)
        {
            // Green is dominant
            if (r < 0.3f && b < 0.4f)
                return "Green";
            else
                return "Yellow"; // Fallback for greenish-yellow
        }
        else if (b > r && b > g)
        {
            // Blue is dominant
            if (r < 0.3f && g < 0.5f)
                return "Blue";
            else
                return "Purple"; // Fallback for bluish colors
        }

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
