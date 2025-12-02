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

    [Tooltip("Reference to BoostItem")]
    public BoostItem boostItem;

    [Tooltip("Reference to InvisibilityItem")]
    public InvisibilityItem invisibilityItem;

    [Header("Current Item")]
    public DefensiveItemType currentItem = DefensiveItemType.None;

    [Header("Item Icons")]
    [Tooltip("Icon sprite for Shield item")]
    public Sprite shieldIcon;

    [Tooltip("Icon sprite for Boost item")]
    public Sprite boostIcon;

    [Tooltip("Icon sprite for Invisibility item")]
    public Sprite invisibilityIcon;

    [Header("AI Control")]
    [Tooltip("If true, AI can trigger item activation via TryUseItemAI()")]
    public bool allowAIControl = false;

    private UFOController ufoController;

    // Defensive item types
    public enum DefensiveItemType
    {
        None,
        Shield,
        Boost,
        Invisibility
    }

    void Start()
    {
        ufoController = GetComponent<UFOController>();

        // Find item components (they should be on same GameObject)
        if (shieldItem == null)
            shieldItem = GetComponent<ShieldItem>();
        if (boostItem == null)
            boostItem = GetComponent<BoostItem>();
        if (invisibilityItem == null)
            invisibilityItem = GetComponent<InvisibilityItem>();

        // Start with no item
        SetItem(DefensiveItemType.None);
    }

    void Update()
    {
        // Skip input if AI controlled
        if (ufoController != null && ufoController.useAIInput)
            return;

        // Check for deploy input - Y button (Button 3) or R key
        bool deployPressed = Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.R);

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

        // Disable all items UNLESS they are currently active
        if (shieldItem != null && !shieldItem.IsShieldActive())
            shieldItem.enabled = false;
        if (boostItem != null && !boostItem.IsBoostActive())
            boostItem.enabled = false;
        if (invisibilityItem != null && !invisibilityItem.IsInvisible())
            invisibilityItem.enabled = false;

        // Enable the selected item
        switch (itemType)
        {
            case DefensiveItemType.Shield:
                if (shieldItem != null)
                {
                    shieldItem.enabled = true;
                }
                break;

            case DefensiveItemType.Boost:
                if (boostItem != null)
                {
                    boostItem.enabled = true;
                }
                break;

            case DefensiveItemType.Invisibility:
                if (invisibilityItem != null)
                {
                    invisibilityItem.enabled = true;
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
                        // Shield is single-use, remove from inventory
                        SetItem(DefensiveItemType.None);
                    }
                }
                break;

            case DefensiveItemType.Boost:
                if (boostItem != null && boostItem.enabled)
                {
                    itemUsed = boostItem.TryActivate();
                    if (itemUsed)
                    {
                        // Boost is instant single-use, remove from inventory
                        SetItem(DefensiveItemType.None);
                    }
                }
                break;

            case DefensiveItemType.Invisibility:
                if (invisibilityItem != null && invisibilityItem.enabled)
                {
                    itemUsed = invisibilityItem.TryActivate();
                    if (itemUsed)
                    {
                        // Invisibility is single-use, remove from inventory
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
            case DefensiveItemType.Boost:
                return "Boost";
            case DefensiveItemType.Invisibility:
                return "Invisibility";
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
            case DefensiveItemType.Boost:
                return boostIcon;
            case DefensiveItemType.Invisibility:
                return invisibilityIcon;
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

            case DefensiveItemType.Boost:
                return boostItem != null && boostItem.enabled && boostItem.CanActivate();

            case DefensiveItemType.Invisibility:
                return invisibilityItem != null && invisibilityItem.enabled && invisibilityItem.CanActivate();

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
