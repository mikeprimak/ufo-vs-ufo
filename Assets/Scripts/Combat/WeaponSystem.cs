using UnityEngine;

/// <summary>
/// Manages weapon firing, ammo, and projectile spawning for UFO
/// Attach to UFO_Player object
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Projectile prefab to spawn when firing")]
    public GameObject projectilePrefab;

    [Tooltip("Where projectiles spawn from (use UFO center if not assigned)")]
    public Transform firePoint;

    [Tooltip("Time between shots (seconds)")]
    public float fireRate = 0.3f;

    [Header("Ammo Settings")]
    [Tooltip("Current ammo count")]
    public int currentAmmo = 20;

    [Tooltip("Maximum ammo capacity")]
    public int maxAmmo = 50;

    [Tooltip("Starting ammo")]
    public int startingAmmo = 20;

    [Header("Audio (Optional)")]
    [Tooltip("Sound to play when firing")]
    public AudioClip fireSound;

    private float lastFireTime;
    private AudioSource audioSource;
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

        // Get or add audio source for fire sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && fireSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f; // Partial 3D sound
        }

        // If no fire point assigned, use UFO center
        if (firePoint == null)
        {
            Debug.LogWarning("WeaponSystem: No fire point assigned, using UFO center");
        }
    }

    /// <summary>
    /// Attempt to fire weapon. Returns true if shot was fired, false if on cooldown or no ammo
    /// </summary>
    public bool TryFire()
    {
        // Check if we can fire
        if (!CanFire())
            return false;

        // Spawn projectile
        Fire();
        return true;
    }

    /// <summary>
    /// Check if weapon is ready to fire
    /// </summary>
    public bool CanFire()
    {
        // Check ammo
        if (currentAmmo <= 0)
        {
            return false;
        }

        // Check fire rate cooldown
        if (Time.time < lastFireTime + fireRate)
            return false;

        // Check if projectile prefab is assigned
        if (projectilePrefab == null)
        {
            Debug.LogError("WeaponSystem: No projectile prefab assigned!");
            return false;
        }

        return true;
    }

    void Fire()
    {
        // Get aiming direction from UFO controller (includes visual pitch)
        UFOController ufoController = GetComponent<UFOController>();
        Quaternion aimDirection = (ufoController != null) ? ufoController.GetAimDirection() : transform.rotation;

        // Determine spawn position and rotation
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (firePoint != null)
        {
            spawnPosition = firePoint.position;
            spawnRotation = firePoint.rotation;
        }
        else
        {
            // Spawn from UFO center, using aim direction
            // Use larger offset (10 units) to accommodate large projectiles like sticky bombs
            spawnPosition = transform.position + (aimDirection * Vector3.forward) * 10f;
            spawnRotation = aimDirection;
        }

        // Spawn projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);

        // Get the root UFO GameObject to set as owner (prevents self-hits)
        GameObject ownerUFO = transform.root.gameObject;

        // Set owner to prevent self-hits (check all projectile types)
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetOwner(ownerUFO);
        }

        HomingProjectile homingProjectileScript = projectile.GetComponent<HomingProjectile>();
        if (homingProjectileScript != null)
        {
            homingProjectileScript.SetOwner(ownerUFO);
        }

        StickyBomb stickyBombScript = projectile.GetComponent<StickyBomb>();
        if (stickyBombScript != null)
        {
            stickyBombScript.SetOwner(ownerUFO);
        }

        // Consume ammo
        currentAmmo--;

        // Update fire time
        lastFireTime = Time.time;

        // Play fire sound
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
    }

    /// <summary>
    /// Add ammo from pickup
    /// </summary>
    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
    }

    /// <summary>
    /// Refill ammo to max
    /// </summary>
    public void RefillAmmo()
    {
        currentAmmo = maxAmmo;
    }
}
