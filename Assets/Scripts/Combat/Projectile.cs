using UnityEngine;

/// <summary>
/// Simple projectile that flies straight forward
/// Handles collision detection and despawning
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Speed of the projectile")]
    public float speed = 50f;

    [Tooltip("How long before projectile destroys itself (seconds)")]
    public float lifetime = 5f;

    [Tooltip("Damage dealt on direct hit")]
    public int damage = 1;

    [Header("Explosion Settings")]
    [Tooltip("Explosion blast radius (0 = no explosion)")]
    public float blastRadius = 40f;

    [Tooltip("Explosion damage to UFOs in blast radius")]
    public int explosionDamage = 1;

    [Tooltip("Explosion visual effect prefab (optional)")]
    public GameObject explosionPrefab;

    [Tooltip("Explosion sound (optional)")]
    public AudioClip explosionSound;

    [Header("Visual Settings")]
    [Tooltip("Optional trail renderer reference")]
    public TrailRenderer trail;

    private Rigidbody rb;
    private float spawnTime;
    private GameObject owner; // Who fired this projectile
    private GameObject directHitTarget = null; // UFO that was directly hit (skip in explosion)
    private bool hasCollided = false; // Prevent multiple collision events

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;

        // Configure rigidbody for projectile physics
        rb.useGravity = false; // Straight flight, no gravity
        rb.drag = 0; // No air resistance
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Fast-moving object

        // Set initial velocity in forward direction
        rb.velocity = transform.forward * speed;
    }

    void Update()
    {
        // Explode when lifetime expires
        if (Time.time >= spawnTime + lifetime)
        {
            Explode();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Prevent multiple collision events for same impact
        if (hasCollided)
            return;
        hasCollided = true;

        // Don't hit the UFO that fired this projectile
        if (collision.gameObject == owner)
            return;

        // Always get the root object first
        GameObject rootObject = collision.gameObject.transform.root.gameObject;

        // Check if we hit a UFO
        if (rootObject.CompareTag("Player"))
        {
            // Store the root UFO that was directly hit (to skip in explosion)
            directHitTarget = rootObject;

            // Hit another UFO - deal direct hit damage with kill attribution
            UFOHealth health = rootObject.GetComponent<UFOHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, owner); // Pass owner so kills are tracked
            }

            // Trigger wobble effect
            UFOHitEffect hitEffect = rootObject.GetComponent<UFOHitEffect>();
            if (hitEffect != null)
            {
                hitEffect.TriggerWobble();
            }
        }

        // Explode on any collision (wall, floor, player)
        Explode();
    }

    void Explode()
    {
        // Skip explosion if blast radius is 0
        if (blastRadius <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Track which UFOs we've already damaged (to avoid hitting same UFO multiple times)
        System.Collections.Generic.HashSet<GameObject> damagedUFOs = new System.Collections.Generic.HashSet<GameObject>();

        // Find all UFOs in blast radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (Collider hit in hitColliders)
        {
            // Check if we hit the root object or a child with Player tag
            if (hit.transform.root.CompareTag("Player"))
            {
                GameObject rootUFO = hit.transform.root.gameObject;

                // Skip the owner
                if (rootUFO == owner)
                    continue;

                // Skip the directly hit UFO (already took collision damage)
                if (rootUFO == directHitTarget)
                    continue;

                // Skip if we already damaged this UFO
                if (damagedUFOs.Contains(rootUFO))
                    continue;

                // Mark this UFO as damaged
                damagedUFOs.Add(rootUFO);

                // Deal explosion damage with kill attribution
                UFOHealth health = rootUFO.GetComponent<UFOHealth>();
                if (health != null)
                {
                    health.TakeDamage(explosionDamage, owner); // Pass owner so kills are tracked
                }

                float distance = Vector3.Distance(transform.position, rootUFO.transform.position);

                // Trigger wobble effect (distance-based intensity)
                UFOHitEffect hitEffect = rootUFO.GetComponent<UFOHitEffect>();
                if (hitEffect != null)
                {
                    float damageFalloff = 1f - (distance / blastRadius);
                    float wobbleIntensity = 15f * damageFalloff;
                    hitEffect.TriggerWobble(wobbleIntensity);
                }

                // Apply explosion knockback force
                Rigidbody targetRb = rootUFO.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    Vector3 explosionDirection = (rootUFO.transform.position - transform.position).normalized;
                    float damageFalloff = 1f - (distance / blastRadius);
                    float explosionForce = 30f * damageFalloff;
                    targetRb.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);
                }
            }
        }

        // Spawn explosion visual effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 5f);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
        }

        // Destroy projectile
        Destroy(gameObject);
    }

    /// <summary>
    /// Set who fired this projectile (to prevent self-hits)
    /// </summary>
    public void SetOwner(GameObject ownerObject)
    {
        owner = ownerObject;
    }
}
