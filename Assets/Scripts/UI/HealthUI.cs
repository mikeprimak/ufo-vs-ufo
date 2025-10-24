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

        Debug.Log($"[HEALTH UI] Created {heartImages.Length} hearts for {targetUFO.name}");
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

            // Use sprite if provided, otherwise use default white sprite
            if (fullHeartSprite != null)
            {
                heartImage.sprite = fullHeartSprite;
            }
            else
            {
                // Use Unity's default UI sprite (white square)
                heartImage.sprite = null;
                heartImage.color = Color.green; // Green for full health
            }

            heartImage.preserveAspect = true;

            // Set size and position
            RectTransform rectTransform = heartObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(heartSize, heartSize);
            rectTransform.anchoredPosition = new Vector2(i * (heartSize + heartSpacing), 0);

            // Store reference
            heartImages[i] = heartImage;

            Debug.Log($"[HEALTH UI] Created heart {i} at position {rectTransform.anchoredPosition}");
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
                // Full health
                if (fullHeartSprite != null)
                {
                    heartImages[i].sprite = fullHeartSprite;
                }
                else
                {
                    heartImages[i].color = Color.green; // Green square for full
                }
            }
            else
            {
                // Empty health
                if (emptyHeartSprite != null)
                {
                    heartImages[i].sprite = emptyHeartSprite;
                }
                else
                {
                    heartImages[i].color = Color.red; // Red square for empty
                }
            }
        }
    }
}
