using UnityEngine;

/// <summary>
/// Simple component to explicitly define a UFO's color name for combat log and UI
/// Attach to each UFO and set the color name in Inspector
/// </summary>
public class UFOColorIdentity : MonoBehaviour
{
    [Tooltip("The color name for this UFO (e.g., 'Red', 'Blue', 'Green', 'Yellow')")]
    public string colorName = "Unknown";

    [Tooltip("The actual color to display in UI (optional, for visual reference)")]
    public Color displayColor = Color.white;

    /// <summary>
    /// Get the color name for this UFO
    /// </summary>
    public string GetColorName()
    {
        return colorName;
    }

    /// <summary>
    /// Get the display color for this UFO
    /// </summary>
    public Color GetDisplayColor()
    {
        return displayColor;
    }
}
