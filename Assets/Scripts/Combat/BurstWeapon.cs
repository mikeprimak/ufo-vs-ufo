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
    public int burstCount = 13;

    [Tooltip("Time between each shot in the burst (seconds)")]
    public float burstDelay = 0.08f;

    [Tooltip("Cooldown after burst completes before can fire again (seconds)")]
    public float cooldown = 2f;

    [Header("Fire Points")]
    [Tooltip("Left side fire point (optional, will create offset if not assigned)")]
    public Transform leftFirePoint;

    [Tooltip("Right side fire point (optional, will create offset if not assigned)")]
    public Transform rightFirePoint;

    [Tooltip("Distance from UFO center for left/right fire points (if not manually assigned)")]
    public float firePointOffset = 2f;

    [Header("Ammo Settings")]
    [Tooltip("Current ammo available")]
    public int currentAmmo = 130;

    [Tooltip("Maximum ammo capacity")]
    public int maxAmmo = 200;

    [Tooltip("Ammo consumed per burst (all 13 shots)")]
    public int ammoPerBurst = 13;

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

    void Start()
    {
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
        // Check for fire input
        // Using Fire2 (same button as missiles/weapons - Button 1 / B-Circle)
        if (Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            TryStartBurst();
        }

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
            Debug.Log("Already firing burst!");
            return false;
        }

        // Check cooldown
        if (Time.time < cooldownEndTime)
        {
            Debug.Log($"Burst weapon on cooldown! {cooldownEndTime - Time.time:F1}s remaining");
            return false;
        }

        // Check ammo
        if (currentAmmo < ammoPerBurst)
        {
            Debug.Log($"Not enough ammo! Need {ammoPerBurst}, have {currentAmmo}");
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

        // Consume ammo
        currentAmmo -= ammoPerBurst;

        Debug.Log($"Burst started! Firing {burstCount} shots. Ammo: {currentAmmo}/{maxAmmo}");
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

        // Set owner to prevent self-hits (check both Projectile and HomingProjectile)
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetOwner(gameObject);
        }

        HomingProjectile homingProjectileScript = projectile.GetComponent<HomingProjectile>();
        if (homingProjectileScript != null)
        {
            homingProjectileScript.SetOwner(gameObject);
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
        Vector3 offset = fireFromLeft ? -transform.right : transform.right;
        return transform.position + offset * firePointOffset;
    }

    void CompleteBurst()
    {
        isBursting = false;
        cooldownEndTime = Time.time + cooldown;

        Debug.Log($"Burst complete! Cooldown: {cooldown}s. Ammo: {currentAmmo}/{maxAmmo}");
    }

    // Public methods for ammo pickups
    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        Debug.Log($"Burst weapon ammo collected! Current: {currentAmmo}/{maxAmmo}");
    }

    public void RefillAmmo()
    {
        currentAmmo = maxAmmo;
        Debug.Log($"Burst weapon ammo refilled! {currentAmmo}/{maxAmmo}");
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
