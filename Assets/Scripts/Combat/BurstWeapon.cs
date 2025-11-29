using UnityEngine;

/// <summary>
/// Hold-to-fire weapon that rapidly fires beam projectiles alternating from left/right sides
/// Each beam fires toward the UFO's current aim direction while fire button is held
/// Ammo is conserved when button is released
/// </summary>
public class BurstWeapon : MonoBehaviour
{
    [Header("Fire Settings")]
    [Tooltip("Projectile prefab to spawn (should be beam-shaped)")]
    public GameObject projectilePrefab;

    [Tooltip("Time between each shot (seconds)")]
    public float fireRate = 0.08f;

    [Header("Fire Points")]
    [Tooltip("Left side fire point (optional, will create offset if not assigned)")]
    public Transform leftFirePoint;

    [Tooltip("Right side fire point (optional, will create offset if not assigned)")]
    public Transform rightFirePoint;

    [Tooltip("Sideways distance from UFO center for left/right fire points")]
    public float firePointOffset = 3f;

    [Tooltip("Forward distance from UFO center for fire points")]
    public float forwardOffset = 5f;

    [Header("Ammo Settings")]
    [Tooltip("Current ammo available")]
    public int currentAmmo = 24;

    [Tooltip("Maximum ammo capacity")]
    public int maxAmmo = 24;

    [Tooltip("Starting ammo (only used if component starts enabled)")]
    public int startingAmmo = 24;

    [Header("Audio (Optional)")]
    [Tooltip("Sound played for each shot in burst")]
    public AudioClip fireSound;

    private AudioSource audioSource;
    private bool isFiring = false; // True while fire button is held
    private float nextShotTime = 0f;
    private bool fireFromLeft = true; // Alternates between left and right
    private UFOController ufoController;
    private bool hasBeenInitialized = false;

    void Awake()
    {
        // Awake runs before Start and before component is enabled
        // Only initialize ammo if component starts enabled (standalone use)
        // If disabled at start, WeaponManager will set ammo before enabling
        if (enabled)
        {
            currentAmmo = startingAmmo;
        }
        hasBeenInitialized = true;
    }

    void Start()
    {
        // If this is called and we haven't set ammo yet, set it now
        // This handles the case where component is enabled after Awake
        if (!hasBeenInitialized)
        {
            currentAmmo = startingAmmo;
            hasBeenInitialized = true;
        }

        ufoController = GetComponent<UFOController>();

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.loop = false;
    }

    void Update()
    {
        // Fire while button is held and we have ammo
        if (isFiring && currentAmmo > 0 && Time.time >= nextShotTime)
        {
            FireSingleShot();
        }
    }

    /// <summary>
    /// Called each frame by WeaponManager to set whether fire button is held
    /// </summary>
    public void SetFireHeld(bool held)
    {
        if (held && !isFiring && currentAmmo > 0 && projectilePrefab != null)
        {
            // Start firing
            isFiring = true;
            nextShotTime = Time.time; // Fire first shot immediately
        }
        else if (!held && isFiring)
        {
            // Stop firing
            isFiring = false;
        }

        // Also stop if out of ammo
        if (currentAmmo <= 0)
        {
            isFiring = false;
        }
    }

    /// <summary>
    /// Legacy method for compatibility - starts firing
    /// </summary>
    public bool TryStartBurst()
    {
        if (currentAmmo > 0 && projectilePrefab != null)
        {
            SetFireHeld(true);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stop firing (call when button released)
    /// </summary>
    public void StopFiring()
    {
        isFiring = false;
    }

    void FireSingleShot()
    {
        // Consume 1 ammo
        currentAmmo--;

        // Get current fire point (alternates between left and right)
        Vector3 spawnPosition = GetCurrentFirePoint();

        // Get aiming direction at THIS moment
        Quaternion aimDirection = (ufoController != null) ?
            ufoController.GetAimDirection() : transform.rotation;

        // Spawn projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, aimDirection);

        // Get the root UFO GameObject to set as owner (prevents self-hits)
        GameObject ownerUFO = transform.root.gameObject;

        // Set owner to prevent self-hits (check both Projectile and HomingProjectile)
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetOwner(ownerUFO);
            // Set custom weapon names for combat log
            projectileScript.SetWeaponNames("Laser Burst", "Laser Burst");
        }

        HomingProjectile homingProjectileScript = projectile.GetComponent<HomingProjectile>();
        if (homingProjectileScript != null)
        {
            homingProjectileScript.SetOwner(ownerUFO);
        }

        // Play fire sound
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }

        // Alternate sides for next shot
        fireFromLeft = !fireFromLeft;

        // Schedule next shot
        nextShotTime = Time.time + fireRate;

        // Stop firing if out of ammo
        if (currentAmmo <= 0)
        {
            isFiring = false;
        }
    }

    Vector3 GetCurrentFirePoint()
    {
        // Use manually assigned fire points if available
        if (fireFromLeft && leftFirePoint != null)
        {
            return leftFirePoint.position;
        }
        else if (!fireFromLeft && rightFirePoint != null)
        {
            return rightFirePoint.position;
        }

        // Otherwise, calculate offset from UFO center
        // Spawn FORWARD and to the side to avoid hitting own UFO
        Vector3 sideOffset = fireFromLeft ? -transform.right : transform.right;
        Vector3 forwardOffsetVec = transform.forward * forwardOffset;
        Vector3 totalOffset = forwardOffsetVec + (sideOffset * firePointOffset);

        return transform.position + totalOffset;
    }

    // Public methods for ammo pickups
    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
    }

    public void RefillAmmo()
    {
        currentAmmo = maxAmmo;
    }

    // Public methods for checking state
    public bool IsFiring()
    {
        return isFiring;
    }

    /// <summary>
    /// Legacy method for compatibility with WeaponManager
    /// </summary>
    public bool IsBursting()
    {
        return isFiring;
    }

    public bool CanFire()
    {
        return currentAmmo > 0;
    }
}
