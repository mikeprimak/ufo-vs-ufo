using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the current weapon on screen
/// Supports both text display and icon/image display
/// Updates automatically when weapon changes
/// </summary>
public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The WeaponManager to monitor")]
    public WeaponManager weaponManager;

    [Header("Text Display (Optional)")]
    [Tooltip("UI Text element to display weapon name (Legacy)")]
    public Text weaponText;

    [Tooltip("TextMeshPro text element (use this OR weaponText above)")]
    public TextMeshProUGUI weaponTextTMP;

    [Header("Icon Display (Optional)")]
    [Tooltip("UI Image element to display weapon icon")]
    public Image weaponIcon;

    [Tooltip("Sprite to show when no weapon equipped")]
    public Sprite noWeaponSprite;

    [Header("Display Settings")]
    [Tooltip("Text to show when no weapon equipped")]
    public string noWeaponText = "NO WEAPON";

    [Tooltip("Prefix before weapon name")]
    public string weaponPrefix = "WEAPON: ";

    void Start()
    {
        // Try to find WeaponManager if not assigned
        if (weaponManager == null)
        {
            weaponManager = FindObjectOfType<WeaponManager>();
        }

        // Try to find Text component if not assigned
        if (weaponText == null && weaponTextTMP == null)
        {
            weaponText = GetComponent<Text>();
            weaponTextTMP = GetComponent<TextMeshProUGUI>();
        }

        if (weaponText == null && weaponTextTMP == null)
        {
            Debug.LogError("WeaponUI: No Text or TextMeshPro component found! Assign one in Inspector.");
        }

        UpdateDisplay();
    }

    void Update()
    {
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (weaponManager == null)
            return;

        // Update text displays
        string displayText;
        if (weaponManager.HasWeapon())
        {
            displayText = weaponPrefix + weaponManager.GetCurrentWeaponName().ToUpper();
            //Debug.Log($"[WEAPON UI] Has weapon: {weaponManager.GetCurrentWeaponName()}");
        }
        else
        {
            displayText = noWeaponText;
            //Debug.Log("[WEAPON UI] No weapon");
        }

        // Update whichever text component exists
        if (weaponText != null)
        {
            weaponText.text = displayText;
        }
        if (weaponTextTMP != null)
        {
            weaponTextTMP.text = displayText;
        }

        // Update icon display
        if (weaponIcon != null)
        {
            Sprite iconSprite = weaponManager.GetCurrentWeaponIcon();

            if (iconSprite != null)
            {
                weaponIcon.sprite = iconSprite;
                weaponIcon.enabled = true; // Show icon when weapon equipped
            }
            else if (noWeaponSprite != null)
            {
                weaponIcon.sprite = noWeaponSprite;
                weaponIcon.enabled = true; // Show "no weapon" icon
            }
            else
            {
                weaponIcon.enabled = false; // Hide icon if no sprite available
            }
        }
    }
}
