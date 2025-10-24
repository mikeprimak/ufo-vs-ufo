using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the current weapon on screen
/// Updates automatically when weapon changes
/// Supports both legacy Text and TextMeshPro
/// </summary>
public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The WeaponManager to monitor")]
    public WeaponManager weaponManager;

    [Tooltip("UI Text element to display weapon name (Legacy)")]
    public Text weaponText;

    [Tooltip("TextMeshPro text element (use this OR weaponText above)")]
    public TextMeshProUGUI weaponTextTMP;

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

        string displayText;
        if (weaponManager.HasWeapon())
        {
            displayText = weaponPrefix + weaponManager.GetCurrentWeaponName().ToUpper();
        }
        else
        {
            displayText = noWeaponText;
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
    }
}
