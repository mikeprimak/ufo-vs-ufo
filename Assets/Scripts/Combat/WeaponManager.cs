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

    [Header("Current Weapon")]
    public WeaponType currentWeapon = WeaponType.None;

    private UFOController ufoController;

    // Weapon types
    public enum WeaponType
    {
        None,
        Missile,
        HomingMissile,
        Laser,
        Burst,
        StickyBomb
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

        if (firePressed)
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
                    weaponSystem.currentAmmo = 1; // Only 1 missile
                    weaponSystem.enabled = true;
                }
                else
                {
                    Debug.LogError("Missile weapon: weaponSystem is null!");
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
                    burstWeapon.currentAmmo = 13; // Exactly 1 burst (13 shots)
                }
                break;

            case WeaponType.StickyBomb:
                if (stickyBombWeaponSystem != null)
                {
                    stickyBombWeaponSystem.currentAmmo = 1; // Only 1 sticky bomb
                    stickyBombWeaponSystem.enabled = true;
                }
                else
                {
                    Debug.LogError("StickyBomb weapon: stickyBombWeaponSystem is null!");
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
                    if (weaponFired && weaponSystem.currentAmmo <= 0)
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
                    if (weaponFired && homingWeaponSystem.currentAmmo <= 0)
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
                if (burstWeapon != null && burstWeapon.enabled)
                {
                    weaponFired = burstWeapon.TryStartBurst();
                    // After firing, immediately remove weapon if ammo depleted
                    if (weaponFired && burstWeapon.currentAmmo <= 0)
                    {
                        // Weapon used, will be removed after burst completes
                        // Don't remove immediately since burst is still firing
                    }
                }
                break;

            case WeaponType.StickyBomb:
                if (stickyBombWeaponSystem != null && stickyBombWeaponSystem.enabled)
                {
                    weaponFired = stickyBombWeaponSystem.TryFire();
                    if (weaponFired && stickyBombWeaponSystem.currentAmmo <= 0)
                    {
                        // Used up the weapon
                        SetWeapon(WeaponType.None);
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
}
