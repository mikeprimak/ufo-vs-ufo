using UnityEngine;

/// <summary>
/// Burst-fire weapon that rapidly fires 13 beam projectiles alternating from left/right sides
/// Each beam fires toward the UFO's current aim direction at time of release
/// </summary>
public class BurstWeapon : MonoBehaviour
{
    [Header("Burst Settings")]
    [Tooltip("Projectile prefab to spawn (should be beam-shaped)")]
    public GameObject projectilePrefab;

    [Tooltip("Number of shots in the burst")]
    public int burstCount = 24;

    [Tooltip("Time between each shot in the burst (seconds)")]
    public float burstDelay = 0.08f;

    [Tooltip("Cooldown after burst completes before can fire again (seconds)")]
    public float cooldown = 2f;

    [Header("Fire Points")]
    [Tooltip("Left side fire point (optional, will create offset if not assigned)")]
    public Transform leftFirePoint;

    [Tooltip("Right side fire point (optional, will create offset if not assigned)")]
    public Transform rightFirePoint;

    [Tooltip("Sideways distance from UFO center for left/right fire points")]
    public float firePointOffset = 3f;

    [Tooltip("Forward distance from UFO center for fire points")]
    public float forwardOffset = 15f;

    [Header("Ammo Settings")]
    [Tooltip("Current ammo available")]
    public int currentAmmo = 240;

    [Tooltip("Maximum ammo capacity")]
    public int maxAmmo = 240;

    [Tooltip("Ammo consumed per burst (all 24 shots)")]
    public int ammoPerBurst = 24;

    [Tooltip("Starting ammo (only used if component starts enabled)")]
    public int startingAmmo = 240;

    [Header("Audio (Optional)")]
    [Tooltip("Sound played for each shot in burst")]
    public AudioClip fireSound;

    private AudioSource audioSource;
    private bool isBursting = false;
    private int shotsRemaining = 0;
    private float nextShotTime = 0f;
    private float cooldownEndTime = 0f;
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
        // WeaponManager handles fire input, we just handle the burst sequence
        // Handle burst firing
        if (isBursting && Time.time >= nextShotTime)
        {
            FireSingleShot();
        }
    }

    public bool TryStartBurst()
    {
        // Check if already bursting
        if (isBursting)
        {
            return false;
        }

        // Check cooldown
        if (Time.time < cooldownEndTime)
        {
            return false;
        }

        // Check ammo
        if (currentAmmo < ammoPerBurst)
        {
            // Debug.Log($"[BURST] Not enough ammo! Need {ammoPerBurst}, have {currentAmmo}");
            return false;
        }

        // Check if projectile prefab is assigned
        if (projectilePrefab == null)
        {
            Debug.LogError("BurstWeapon: No projectile prefab assigned!");
            return false;
        }

        // Start burst
        StartBurst();
        return true;
    }

    void StartBurst()
    {
        isBursting = true;
        shotsRemaining = burstCount;
        nextShotTime = Time.time; // Fire first shot immediately
        fireFromLeft = true; // Start from left side

        // Consume ammo (KEEP THIS LOG for troubleshooting burst weapon issue)
        Debug.Log($"[BURST] Starting burst. Ammo before: {currentAmmo}");
        currentAmmo -= ammoPerBurst;
        Debug.Log($"[BURST] Burst started! Ammo after consuming {ammoPerBurst}: {currentAmmo}");
    }

    void FireSingleShot()
    {
        // Get current fire point (alternates between left and right)
        Vector3 spawnPosition = GetCurrentFirePoint();

        // Get aiming direction at THIS moment (not when burst started)
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

        // Decrement remaining shots
        shotsRemaining--;

        // Check if burst is complete
        if (shotsRemaining <= 0)
        {
            CompleteBurst();
        }
        else
        {
            // Schedule next shot
            nextShotTime = Time.time + burstDelay;
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

    void CompleteBurst()
    {
        isBursting = false;
        cooldownEndTime = Time.time + cooldown;

        Debug.Log($"[BURST] Burst complete! Ammo remaining: {currentAmmo}");
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
    public bool IsBursting()
    {
        return isBursting;
    }

    public bool CanFire()
    {
        return !isBursting && Time.time >= cooldownEndTime && currentAmmo >= ammoPerBurst;
    }
}
