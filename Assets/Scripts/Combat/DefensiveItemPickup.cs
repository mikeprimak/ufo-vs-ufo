using UnityEngine;

/// <summary>
/// Defensive item pickup box that gives player 1 use of a defensive item
/// Floats/rotates for visibility, respawns after time
/// Mirrors WeaponPickup but for defensive items
/// </summary>
public class DefensiveItemPickup : MonoBehaviour
{
    [Header("Item Type")]
    [Tooltip("Enable to give a random defensive item instead of a specific one")]
    public bool randomItem = false;

    [Tooltip("Which defensive item this pickup gives (ignored if randomItem is true)")]
    public DefensiveItemManager.DefensiveItemType itemType = DefensiveItemManager.DefensiveItemType.Shield;

    [Header("Respawn Settings")]
    [Tooltip("Does this pickup respawn after being collected?")]
    public bool respawns = true;

    [Tooltip("Time before respawning (seconds)")]
    public float respawnTime = 15f;

    [Header("Visual Settings")]
    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 90f;

    [Tooltip("Bob up/down speed")]
    public float bobSpeed = 3f;

    [Tooltip("Bob height")]
    public float bobHeight = 0.5f;

    [Header("Pickup Settings")]
    [Tooltip("Visual scale multiplier for pickup box")]
    public float visualScale = 1.5f;

    [Tooltip("Trigger collider radius multiplier (increase for easier pickup)")]
    public float triggerSizeMultiplier = 1f;

    private MeshRenderer pickupRenderer;
    private Collider pickupCollider;
    private bool isAvailable = true;
    private float respawnTimer = 0f;
    private Vector3 startPosition;
    private float bobTimer = 0f;

    // Reservation system for AI
    private GameObject claimedBy = null;
    private float claimTime = 0f;
    private float claimTimeout = 5f; // Release claim after 5 seconds if not collected

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

            // Increase trigger size for easier pickup
            if (pickupCollider is SphereCollider)
            {
                SphereCollider sphere = (SphereCollider)pickupCollider;
                sphere.radius *= triggerSizeMultiplier;
            }
            else if (pickupCollider is BoxCollider)
            {
                BoxCollider box = (BoxCollider)pickupCollider;
                box.size *= triggerSizeMultiplier;
            }
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

        // Handle claim timeout - release claim if AI takes too long
        if (claimedBy != null)
        {
            // Check if claimer was destroyed
            if (claimedBy == null)
            {
                ReleaseClaim();
            }
            // Check timeout
            else if (Time.time >= claimTime + claimTimeout)
            {
                ReleaseClaim();
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
        DefensiveItemManager itemManager = other.GetComponent<DefensiveItemManager>();
        if (itemManager != null)
        {
            // Determine which item to give
            DefensiveItemManager.DefensiveItemType itemToGive = randomItem ? GetRandomItemType() : itemType;

            Debug.Log($"[DEFENSIVE ITEM PICKUP] {other.gameObject.name} picked up {itemToGive} from {gameObject.name} (random={randomItem}, position={transform.position})");

            // Give item to player
            itemManager.PickupItem(itemToGive);

            // Pickup collected
            Collect();
        }
    }

    /// <summary>
    /// Get a random defensive item type (excluding None)
    /// </summary>
    DefensiveItemManager.DefensiveItemType GetRandomItemType()
    {
        // Get all item types except None
        System.Array allItems = System.Enum.GetValues(typeof(DefensiveItemManager.DefensiveItemType));

        // Create array of valid items (exclude None which is index 0)
        DefensiveItemManager.DefensiveItemType[] validItems = new DefensiveItemManager.DefensiveItemType[allItems.Length - 1];
        int index = 0;
        foreach (DefensiveItemManager.DefensiveItemType item in allItems)
        {
            if (item != DefensiveItemManager.DefensiveItemType.None)
            {
                validItems[index] = item;
                index++;
            }
        }

        // Pick random item
        int randomIndex = Random.Range(0, validItems.Length);
        return validItems[randomIndex];
    }

    void Collect()
    {
        isAvailable = false;

        // Release claim when collected
        ReleaseClaim();

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

        // Release any existing claims when respawning
        ReleaseClaim();

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

    /// <summary>
    /// Check if this pickup is currently available (not respawning)
    /// </summary>
    public bool IsAvailable()
    {
        return isAvailable;
    }

    /// <summary>
    /// Check if this pickup is claimed by someone else
    /// </summary>
    public bool IsClaimedByOther(GameObject requester)
    {
        return claimedBy != null && claimedBy != requester;
    }

    /// <summary>
    /// Try to claim this pickup for AI targeting
    /// </summary>
    public bool TryClaim(GameObject claimer)
    {
        // Can't claim if not available
        if (!isAvailable)
            return false;

        // Can't claim if already claimed by someone else
        if (claimedBy != null && claimedBy != claimer)
            return false;

        // Claim it
        claimedBy = claimer;
        claimTime = Time.time;
        return true;
    }

    /// <summary>
    /// Release claim on this pickup
    /// </summary>
    public void ReleaseClaim()
    {
        claimedBy = null;
        claimTime = 0f;
    }

    /// <summary>
    /// Release claim if it was claimed by specific GameObject
    /// </summary>
    public void ReleaseClaimBy(GameObject claimer)
    {
        if (claimedBy == claimer)
        {
            ReleaseClaim();
        }
    }
}
