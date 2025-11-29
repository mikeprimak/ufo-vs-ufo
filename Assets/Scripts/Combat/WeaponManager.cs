using UnityEngine;

/// <summary>
/// Manages weapon inventory and switching for UFO
/// UFO starts with no weapons, must pick up weapon boxes
/// Each pickup gives 1 use of that weapon
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Components")]
    [Tooltip("Reference to WeaponSystem (basic bullet)")]
    public WeaponSystem weaponSystem;

    [Tooltip("Reference to HomingProjectile spawner (will create if needed)")]
    public WeaponSystem homingWeaponSystem;

    [Tooltip("Reference to LaserWeapon")]
    public LaserWeapon laserWeapon;

    [Tooltip("Reference to BurstWeapon")]
    public BurstWeapon burstWeapon;

    [Tooltip("Reference to StickyBomb weapon system")]
    public WeaponSystem stickyBombWeaponSystem;

    [Tooltip("Reference to Dash weapon")]
    public DashWeapon dashWeapon;

    [Header("Current Weapon")]
    public WeaponType currentWeapon = WeaponType.None;

    [Header("Weapon Icons")]
    [Tooltip("Icon sprite for Missile weapon")]
    public Sprite missileIcon;

    [Tooltip("Icon sprite for Homing Missile weapon")]
    public Sprite homingMissileIcon;

    [Tooltip("Icon sprite for Laser weapon")]
    public Sprite laserIcon;

    [Tooltip("Icon sprite for Burst weapon")]
    public Sprite burstIcon;

    [Tooltip("Icon sprite for Sticky Bomb weapon")]
    public Sprite stickyBombIcon;

    [Tooltip("Icon sprite for Dash weapon")]
    public Sprite dashIcon;

    [Header("AI Control")]
    [Tooltip("If true, AI can trigger weapon firing via TryFireWeaponAI()")]
    public bool allowAIControl = false;

    private UFOController ufoController;

    // Weapon types
    public enum WeaponType
    {
        None,
        Missile,
        HomingMissile,
        Laser,
        Burst,
        StickyBomb,
        Dash
    }

    void Start()
    {
        ufoController = GetComponent<UFOController>();

        // Find weapon components (they should be on same GameObject)
        if (weaponSystem == null)
            weaponSystem = GetComponent<WeaponSystem>();
        if (laserWeapon == null)
            laserWeapon = GetComponent<LaserWeapon>();
        if (burstWeapon == null)
            burstWeapon = GetComponent<BurstWeapon>();

        // Start with no weapon
        SetWeapon(WeaponType.None);
    }

    void Update()
    {
        // Check for fire input
        bool firePressed = Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.JoystickButton1);
        bool fireHeld = Input.GetButton("Fire2") || Input.GetKey(KeyCode.JoystickButton1);

        // Burst weapon uses hold-to-fire
        if (currentWeapon == WeaponType.Burst && burstWeapon != null && burstWeapon.enabled)
        {
            burstWeapon.SetFireHeld(fireHeld);

            // Check if out of ammo
            if (burstWeapon.currentAmmo <= 0)
            {
                SetWeapon(WeaponType.None);
            }
        }
        else if (firePressed)
        {
            TryFireCurrentWeapon();
        }
    }

    /// <summary>
    /// Pickup a weapon (gives 1 use)
    /// </summary>
    public void PickupWeapon(WeaponType weaponType)
    {
        SetWeapon(weaponType);

        // Log weapon pickup to combat log (only for human player to reduce spam)
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && stats.isHumanPlayer && weaponType != WeaponType.None)
        {
            CombatLogUI.LogWeaponPickup(gameObject, weaponType.ToString());
        }
    }

    /// <summary>
    /// Set the current active weapon
    /// </summary>
    void SetWeapon(WeaponType weaponType)
    {
        currentWeapon = weaponType;

        // Disable all weapons
        if (weaponSystem != null)
            weaponSystem.enabled = false;
        if (homingWeaponSystem != null)
            homingWeaponSystem.enabled = false;
        if (laserWeapon != null)
            laserWeapon.enabled = false;
        if (burstWeapon != null)
            burstWeapon.enabled = false;
        if (stickyBombWeaponSystem != null)
            stickyBombWeaponSystem.enabled = false;

        // Enable the selected weapon and set ammo for 1 use
        switch (weaponType)
        {
            case WeaponType.Missile:
                if (weaponSystem != null)
                {
                    // Set ammo BEFORE enabling (prevents Start() from interfering)
                    weaponSystem.currentAmmo = 3; // 3 missiles per pickup
                    weaponSystem.enabled = true;
                }
                break;

            case WeaponType.HomingMissile:
                if (homingWeaponSystem != null)
                {
                    homingWeaponSystem.enabled = true;
                    homingWeaponSystem.currentAmmo = 1; // Only 1 homing missile
                }
                break;

            case WeaponType.Laser:
                if (laserWeapon != null)
                {
                    laserWeapon.enabled = true;
                    // Laser is time-based, no ammo needed
                }
                break;

            case WeaponType.Burst:
                if (burstWeapon != null)
                {
                    burstWeapon.enabled = true;
                    burstWeapon.currentAmmo = 75;
                }
                break;

            case WeaponType.StickyBomb:
                if (stickyBombWeaponSystem != null)
                {
                    stickyBombWeaponSystem.currentAmmo = 1; // Only 1 sticky bomb
                    stickyBombWeaponSystem.enabled = true;
                }
                break;

            case WeaponType.Dash:
                if (dashWeapon != null)
                {
                    dashWeapon.enabled = true;
                }
                break;

            case WeaponType.None:
                // No weapon active
                break;
        }
    }

    /// <summary>
    /// Try to fire the current weapon
    /// </summary>
    void TryFireCurrentWeapon()
    {
        bool weaponFired = false;

        switch (currentWeapon)
        {
            case WeaponType.Missile:
                if (weaponSystem != null && weaponSystem.enabled)
                {
                    weaponFired = weaponSystem.TryFire();

                    // Check ammo regardless of whether shot fired (ammo decreases even on failed shots)
                    if (weaponSystem.currentAmmo <= 0)
                    {
                        // Used up the weapon
                        SetWeapon(WeaponType.None);
                    }
                }
                break;

            case WeaponType.HomingMissile:
                if (homingWeaponSystem != null && homingWeaponSystem.enabled)
                {
                    weaponFired = homingWeaponSystem.TryFire();

                    // Check ammo regardless of whether shot fired
                    if (homingWeaponSystem.currentAmmo <= 0)
                    {
                        // Used up the weapon
                        SetWeapon(WeaponType.None);
                    }
                }
                break;

            case WeaponType.Laser:
                if (laserWeapon != null && laserWeapon.enabled)
                {
                    weaponFired = laserWeapon.TryFire();
                    if (weaponFired)
                    {
                        // Laser is single-use, remove after firing
                        // Will auto-deactivate after duration
                        // We'll check when it's done in LateUpdate
                    }
                }
                break;

            case WeaponType.Burst:
                // Burst weapon uses hold-to-fire, handled in Update()
                // This case is only reached by AI via TryFireWeaponAI()
                if (burstWeapon != null && burstWeapon.enabled)
                {
                    burstWeapon.SetFireHeld(true);
                    weaponFired = true;
                }
                break;

            case WeaponType.StickyBomb:
                if (stickyBombWeaponSystem != null && stickyBombWeaponSystem.enabled)
                {
                    weaponFired = stickyBombWeaponSystem.TryFire();

                    // Check ammo regardless of whether shot fired
                    if (stickyBombWeaponSystem.currentAmmo <= 0)
                    {
                        // Used up the weapon
                        SetWeapon(WeaponType.None);
                    }
                }
                break;

            case WeaponType.Dash:
                if (dashWeapon != null && dashWeapon.enabled)
                {
                    weaponFired = dashWeapon.TryFire();

                    // Dash activates immediately and lasts for duration
                    // Will auto-disable when duration ends
                    if (weaponFired)
                    {
                        // Weapon activated successfully
                        // Will be removed when dash ends (handled in DashWeapon)
                    }
                }
                break;

            case WeaponType.None:
                // No weapon equipped
                break;
        }
    }

    void LateUpdate()
    {
        // Check if laser finished (single use)
        if (currentWeapon == WeaponType.Laser && laserWeapon != null)
        {
            if (!laserWeapon.IsActive() && laserWeapon.enabled)
            {
                // Laser finished, check if it was ever activated
                // If cooldown exists, it was used
                if (!laserWeapon.CanFire())
                {
                    SetWeapon(WeaponType.None);
                }
            }
        }

        // Check if burst finished (single use)
        if (currentWeapon == WeaponType.Burst && burstWeapon != null)
        {
            if (!burstWeapon.IsBursting() && burstWeapon.currentAmmo <= 0)
            {
                SetWeapon(WeaponType.None);
            }
        }
    }

    /// <summary>
    /// Get the current weapon name for UI display
    /// </summary>
    public string GetCurrentWeaponName()
    {
        switch (currentWeapon)
        {
            case WeaponType.Missile:
                return "Missile";
            case WeaponType.HomingMissile:
                return "Homing Missile";
            case WeaponType.Laser:
                return "Laser";
            case WeaponType.Burst:
                return "Burst Cannon";
            case WeaponType.StickyBomb:
                return "Sticky Bomb";
            case WeaponType.Dash:
                return "Dash";
            case WeaponType.None:
                return "No Weapon";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// Check if player has a weapon
    /// </summary>
    public bool HasWeapon()
    {
        return currentWeapon != WeaponType.None;
    }

    /// <summary>
    /// Get the current weapon icon sprite for UI display
    /// </summary>
    public Sprite GetCurrentWeaponIcon()
    {
        switch (currentWeapon)
        {
            case WeaponType.Missile:
                return missileIcon;
            case WeaponType.HomingMissile:
                return homingMissileIcon;
            case WeaponType.Laser:
                return laserIcon;
            case WeaponType.Burst:
                return burstIcon;
            case WeaponType.StickyBomb:
                return stickyBombIcon;
            case WeaponType.Dash:
                return dashIcon;
            case WeaponType.None:
            default:
                return null; // No icon for no weapon
        }
    }

    /// <summary>
    /// Check if the current weapon can fire (for aim indicator visibility)
    /// </summary>
    public bool CanCurrentWeaponFire()
    {
        switch (currentWeapon)
        {
            case WeaponType.Missile:
                return weaponSystem != null && weaponSystem.enabled && weaponSystem.CanFire();

            case WeaponType.HomingMissile:
                return homingWeaponSystem != null && homingWeaponSystem.enabled && homingWeaponSystem.CanFire();

            case WeaponType.Laser:
                return laserWeapon != null && laserWeapon.enabled && laserWeapon.CanFire();

            case WeaponType.Burst:
                return burstWeapon != null && burstWeapon.enabled && burstWeapon.CanFire();

            case WeaponType.StickyBomb:
                return stickyBombWeaponSystem != null && stickyBombWeaponSystem.enabled && stickyBombWeaponSystem.CanFire();

            case WeaponType.Dash:
                return dashWeapon != null && dashWeapon.enabled && dashWeapon.CanFire();

            case WeaponType.None:
            default:
                return false;
        }
    }

    /// <summary>
    /// AI method to try firing current weapon
    /// </summary>
    public void TryFireWeaponAI()
    {
        if (!allowAIControl)
            return;

        TryFireCurrentWeapon();
    }
}
