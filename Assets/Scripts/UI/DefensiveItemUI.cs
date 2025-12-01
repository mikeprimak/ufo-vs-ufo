using UnityEngine;
using TMPro;

/// <summary>
/// Simple UI to display current defensive item
/// Shows item name in corner of screen
/// </summary>
public class DefensiveItemUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player UFO to monitor")]
    public GameObject playerUFO;

    [Header("UI Elements")]
    [Tooltip("Text to display defensive item name")]
    public TextMeshProUGUI itemText;

    [Header("Display Settings")]
    [Tooltip("Text to show when no item equipped")]
    public string noItemText = "[RB] No Item";

    [Tooltip("Format for item name (use {0} for item name)")]
    public string itemFormat = "[RB] {0}";

    private DefensiveItemManager itemManager;

    void Start()
    {
        // Find player UFO if not assigned
        if (playerUFO == null)
        {
            // Try to find player UFO by name
            playerUFO = GameObject.Find("UFO_Player");
        }

        // Get DefensiveItemManager from player
        if (playerUFO != null)
        {
            itemManager = playerUFO.GetComponent<DefensiveItemManager>();
        }

        if (itemManager == null)
        {
            Debug.LogWarning("[DefensiveItemUI] No DefensiveItemManager found on player UFO!");
        }
    }

    void Update()
    {
        if (itemText == null)
            return;

        if (itemManager == null)
        {
            itemText.text = noItemText;
            return;
        }

        // Update text based on current item
        if (itemManager.HasItem())
        {
            string itemName = itemManager.GetCurrentItemName();
            itemText.text = string.Format(itemFormat, itemName);
        }
        else
        {
            itemText.text = noItemText;
        }
    }
}
