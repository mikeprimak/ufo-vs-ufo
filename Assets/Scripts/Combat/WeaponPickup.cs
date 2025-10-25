using UnityEngine;

/// <summary>
/// Weapon pickup box that gives player 1 use of a weapon
/// Floats/rotates for visibility, respawns after time
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Type")]
    [Tooltip("Enable to give a random weapon instead of a specific one")]
    public bool randomWeapon = false;

    [Tooltip("Which weapon this pickup gives (ignored if randomWeapon is true)")]
    public WeaponManager.WeaponType weaponType = WeaponManager.WeaponType.Missile;

    [Header("Respawn Settings")]
    [Tooltip("Does this pickup respawn after being collected?")]
    public bool respawns = true;

    [Tooltip("Time before respawning (seconds)")]
    public float respawnTime = 15f;

    [Header("Visual Settings")]
    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 90f;

    [Tooltip("Bob up/down speed")]
    public float bobSpeed = 1f;

    [Tooltip("Bob height")]
    public float bobHeight = 0.5f;

    private MeshRenderer pickupRenderer;
    private Collider pickupCollider;
    private bool isAvailable = true;
    private float respawnTimer = 0f;
    private Vector3 startPosition;
    private float bobTimer = 0f;

    void Start()
    {
        // Get components (will auto-find if not assigned)
        pickupRenderer = GetComponentInChildren<MeshRenderer>();
        if (pickupRenderer == null)
            pickupRenderer = GetComponent<MeshRenderer>();

        pickupCollider = GetComponent<Collider>();

        // Store starting position for bobbing
        startPosition = transform.position;

        // Ensure we have a trigger collider
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    void Update()
    {
        // Rotate the pickup
        if (isAvailable)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // Bob up and down
            bobTimer += Time.deltaTime * bobSpeed;
            float newY = startPosition.y + Mathf.Sin(bobTimer) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }

        // Handle respawn timer
        if (!isAvailable && respawns)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only pickup if available
        if (!isAvailable)
        {
            return;
        }

        // Check if a UFO picked this up
        WeaponManager weaponManager = other.GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            // Determine which weapon to give
            WeaponManager.WeaponType weaponToGive = randomWeapon ? GetRandomWeaponType() : weaponType;

            // Give weapon to player
            weaponManager.PickupWeapon(weaponToGive);

            // Pickup collected
            Collect();
        }
    }

    /// <summary>
    /// Get a random weapon type (excluding None)
    /// </summary>
    WeaponManager.WeaponType GetRandomWeaponType()
    {
        // Get all weapon types except None
        System.Array allWeapons = System.Enum.GetValues(typeof(WeaponManager.WeaponType));

        // Create array of valid weapons (exclude None which is index 0)
        WeaponManager.WeaponType[] validWeapons = new WeaponManager.WeaponType[allWeapons.Length - 1];
        int index = 0;
        foreach (WeaponManager.WeaponType weapon in allWeapons)
        {
            if (weapon != WeaponManager.WeaponType.None)
            {
                validWeapons[index] = weapon;
                index++;
            }
        }

        // Pick random weapon
        int randomIndex = Random.Range(0, validWeapons.Length);
        return validWeapons[randomIndex];
    }

    void Collect()
    {
        isAvailable = false;

        // Hide visual
        if (pickupRenderer != null)
            pickupRenderer.enabled = false;

        // Disable collider
        if (pickupCollider != null)
            pickupCollider.enabled = false;

        // Start respawn timer
        if (respawns)
        {
            respawnTimer = respawnTime;
        }
        else
        {
            // Destroy if not respawning
            Destroy(gameObject);
        }
    }

    void Respawn()
    {
        isAvailable = true;

        // Show visual
        if (pickupRenderer != null)
            pickupRenderer.enabled = true;

        // Enable collider
        if (pickupCollider != null)
            pickupCollider.enabled = true;

        // Reset bob timer
        bobTimer = 0f;
        transform.position = startPosition;
    }
}
