using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a boost meter UI that drains when boosting and recharges over time
/// Shows boost availability for the player's UFO
/// </summary>
public class BoostMeter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's UFO controller to read boost from")]
    public UFOController playerController;

    [Tooltip("UI Image for the boost meter fill")]
    public Image fillImage;

    [Header("Visual Settings")]
    [Tooltip("Color when boost is full/charging")]
    public Color fullColor = new Color(0f, 1f, 1f, 1f); // Cyan

    [Tooltip("Color when boost is low")]
    public Color lowColor = new Color(1f, 0.5f, 0f, 1f); // Orange

    [Tooltip("Color when boost is depleted")]
    public Color emptyColor = new Color(1f, 0f, 0f, 1f); // Red

    [Tooltip("Threshold for 'low' color (0-1)")]
    public float lowThreshold = 0.3f;

    void Update()
    {
        if (playerController == null || fillImage == null)
            return;

        // Get boost percentage from player controller
        float boostPercent = playerController.GetBoostPercent();

        // Update fill amount
        fillImage.fillAmount = boostPercent;

        // Update color based on boost level
        if (boostPercent <= 0f)
        {
            fillImage.color = emptyColor;
        }
        else if (boostPercent < lowThreshold)
        {
            // Lerp between empty and low color
            float t = boostPercent / lowThreshold;
            fillImage.color = Color.Lerp(emptyColor, lowColor, t);
        }
        else
        {
            // Lerp between low and full color
            float t = (boostPercent - lowThreshold) / (1f - lowThreshold);
            fillImage.color = Color.Lerp(lowColor, fullColor, t);
        }
    }
}
