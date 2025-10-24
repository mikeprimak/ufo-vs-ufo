using UnityEngine;

/// <summary>
/// Ammo pickup that refills player's weapon ammo
/// Attach to a pickup object in the arena
/// </summary>
public class AmmoPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Amount of ammo to give")]
    public int ammoAmount = 10;

    [Tooltip("Should pickup respawn after being collected?")]
    public bool respawns = true;

    [Tooltip("Time before respawn (seconds)")]
    public float respawnTime = 10f;

    [Header("Visual Settings")]
    [Tooltip("Renderer to hide/show on pickup")]
    public Renderer pickupRenderer;

    [Tooltip("Collider to disable when picked up")]
    public Collider pickupCollider;

    [Tooltip("Rotation speed for visual spin (degrees per second)")]
    public float rotationSpeed = 90f;

    private bool isAvailable = true;
    private float respawnTimer;

    void Start()
    {
        // Auto-find components if not assigned
        if (pickupRenderer == null)
            pickupRenderer = GetComponent<Renderer>();

        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider>();
    }

    void Update()
    {
        // Spin the pickup for visual effect
        if (isAvailable)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        // Handle respawn timer
        if (!isAvailable && respawns)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0)
            {
                Respawn();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only pickup if available
        if (!isAvailable)
            return;

        // Check if a UFO picked this up
        WeaponSystem weaponSystem = other.GetComponent<WeaponSystem>();
        if (weaponSystem != null)
        {
            // Give ammo
            weaponSystem.AddAmmo(ammoAmount);

            // Pickup collected
            Collect();
        }
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

        Debug.Log("Ammo pickup respawned!");
    }
}
