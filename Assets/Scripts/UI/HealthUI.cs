using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays UFO health as a row of hearts/icons in the UI
/// Updates automatically based on UFOHealth component
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The UFO to track health for")]
    public UFOHealth targetUFO;

    [Tooltip("Sprite to show for full health point")]
    public Sprite fullHeartSprite;

    [Tooltip("Sprite to show for empty health point")]
    public Sprite emptyHeartSprite;

    [Tooltip("Size of each heart icon (pixels)")]
    public float heartSize = 50f;

    [Tooltip("Spacing between hearts (pixels)")]
    public float heartSpacing = 10f;

    // Heart image objects
    private Image[] heartImages;

    void Start()
    {
        if (targetUFO == null)
        {
            Debug.LogError("[HEALTH UI] No target UFO assigned!");
            return;
        }

        // Create heart icons based on max health
        CreateHeartIcons();
    }

    void Update()
    {
        if (targetUFO == null || heartImages == null)
            return;

        // Update heart display based on current health
        UpdateHeartDisplay();
    }

    /// <summary>
    /// Create the heart icon UI elements
    /// </summary>
    void CreateHeartIcons()
    {
        int maxHealth = targetUFO.GetMaxHealth();
        heartImages = new Image[maxHealth];

        for (int i = 0; i < maxHealth; i++)
        {
            // Create heart GameObject
            GameObject heartObj = new GameObject($"Heart_{i}");
            heartObj.transform.SetParent(transform, false);

            // Add Image component
            Image heartImage = heartObj.AddComponent<Image>();
            heartImage.sprite = fullHeartSprite;
            heartImage.preserveAspect = true;

            // Set size and position
            RectTransform rectTransform = heartObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(heartSize, heartSize);
            rectTransform.anchoredPosition = new Vector2(i * (heartSize + heartSpacing), 0);

            // Store reference
            heartImages[i] = heartImage;
        }
    }

    /// <summary>
    /// Update heart sprites based on current health
    /// </summary>
    void UpdateHeartDisplay()
    {
        int currentHealth = targetUFO.GetCurrentHealth();

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
                continue;

            // Show full heart if index < current health, empty heart otherwise
            if (i < currentHealth)
            {
                heartImages[i].sprite = fullHeartSprite;
            }
            else
            {
                heartImages[i].sprite = emptyHeartSprite;
            }
        }
    }
}
