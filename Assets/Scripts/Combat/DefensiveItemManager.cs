using UnityEngine;

/// <summary>
/// Manages defensive item inventory and activation for UFO
/// UFO starts with no defensive items, must pick up item boxes
/// Each pickup gives 1 use of that item
/// Mirrors WeaponManager but for defensive abilities
/// </summary>
public class DefensiveItemManager : MonoBehaviour
{
    [Header("Defensive Item Components")]
    [Tooltip("Reference to ShieldItem")]
    public ShieldItem shieldItem;

    [Header("Current Item")]
    public DefensiveItemType currentItem = DefensiveItemType.None;

    [Header("Item Icons")]
    [Tooltip("Icon sprite for Shield item")]
    public Sprite shieldIcon;

    [Header("AI Control")]
    [Tooltip("If true, AI can trigger item activation via TryUseItemAI()")]
    public bool allowAIControl = false;

    private UFOController ufoController;

    // Defensive item types
    public enum DefensiveItemType
    {
        None,
        Shield
        // Future: Decoy, Smoke, etc.
    }

    void Start()
    {
        ufoController = GetComponent<UFOController>();

        // Find item components (they should be on same GameObject)
        if (shieldItem == null)
            shieldItem = GetComponent<ShieldItem>();

        // Start with no item
        SetItem(DefensiveItemType.None);
    }

    void Update()
    {
        // Skip input if AI controlled
        if (ufoController != null && ufoController.useAIInput)
            return;

        // Check for deploy input - Right Bumper (RB / Button 5)
        bool deployPressed = Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.R);

        if (deployPressed)
        {
            Debug.Log($"[DEFENSIVE] Deploy pressed! Current item: {currentItem}, shieldItem null: {shieldItem == null}");
            if (shieldItem != null)
            {
                Debug.Log($"[DEFENSIVE] ShieldItem enabled: {shieldItem.enabled}, CanActivate: {shieldItem.CanActivate()}");
            }
            TryUseCurrentItem();
        }
    }

    /// <summary>
    /// Pickup a defensive item (gives 1 use)
    /// </summary>
    public void PickupItem(DefensiveItemType itemType)
    {
        SetItem(itemType);

        // Log item pickup to combat log (only for human player to reduce spam)
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && stats.isHumanPlayer && itemType != DefensiveItemType.None)
        {
            CombatLogUI.LogWeaponPickup(gameObject, itemType.ToString());
        }
    }

    /// <summary>
    /// Set the current active defensive item
    /// </summary>
    void SetItem(DefensiveItemType itemType)
    {
        currentItem = itemType;

        // Disable all items UNLESS they are currently active (e.g., shield is up)
        if (shieldItem != null && !shieldItem.IsShieldActive())
            shieldItem.enabled = false;

        // Enable the selected item
        switch (itemType)
        {
            case DefensiveItemType.Shield:
                if (shieldItem != null)
                {
                    shieldItem.enabled = true;
                }
                break;

            case DefensiveItemType.None:
                // No item active
                break;
        }
    }

    /// <summary>
    /// Try to use the current defensive item
    /// </summary>
    void TryUseCurrentItem()
    {
        bool itemUsed = false;

        switch (currentItem)
        {
            case DefensiveItemType.Shield:
                if (shieldItem != null && shieldItem.enabled)
                {
                    itemUsed = shieldItem.TryActivate();
                    if (itemUsed)
                    {
                        // Shield is single-use, will auto-disable when duration ends
                        // Remove from inventory immediately after activation
                        SetItem(DefensiveItemType.None);
                    }
                }
                break;

            case DefensiveItemType.None:
                // No item equipped
                break;
        }
    }

    /// <summary>
    /// Get the current item name for UI display
    /// </summary>
    public string GetCurrentItemName()
    {
        switch (currentItem)
        {
            case DefensiveItemType.Shield:
                return "Shield";
            case DefensiveItemType.None:
                return "No Item";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// Check if player has a defensive item
    /// </summary>
    public bool HasItem()
    {
        return currentItem != DefensiveItemType.None;
    }

    /// <summary>
    /// Get the current item icon sprite for UI display
    /// </summary>
    public Sprite GetCurrentItemIcon()
    {
        switch (currentItem)
        {
            case DefensiveItemType.Shield:
                return shieldIcon;
            case DefensiveItemType.None:
            default:
                return null;
        }
    }

    /// <summary>
    /// Check if the current item can be used
    /// </summary>
    public bool CanCurrentItemBeUsed()
    {
        switch (currentItem)
        {
            case DefensiveItemType.Shield:
                return shieldItem != null && shieldItem.enabled && shieldItem.CanActivate();

            case DefensiveItemType.None:
            default:
                return false;
        }
    }

    /// <summary>
    /// AI method to try using current defensive item
    /// </summary>
    public void TryUseItemAI()
    {
        if (!allowAIControl)
            return;

        TryUseCurrentItem();
    }
}
