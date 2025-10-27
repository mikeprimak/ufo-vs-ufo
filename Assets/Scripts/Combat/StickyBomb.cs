using UnityEngine;

/// <summary>
/// Sticky time bomb projectile that travels slowly, sticks to surfaces, and explodes after a timer
/// Does not detonate on impact - only on timer expiry
/// Damages UFOs on contact but continues flying
/// </summary>
public class StickyBomb : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Forward travel speed (slightly faster than max UFO speed)")]
    public float speed = 35f;

    [Header("Timer")]
    [Tooltip("Time before explosion (seconds)")]
    public float fuseTime = 5f;

    [Header("Explosion")]
    [Tooltip("Explosion damage amount (flat damage to all UFOs in radius)")]
    public int explosionDamage = 1;

    [Tooltip("Explosion blast radius")]
    public float blastRadius = 90f;

    [Tooltip("Explosion visual effect prefab (optional)")]
    public GameObject explosionPrefab;

    [Header("Contact Damage")]
    [Tooltip("Damage dealt when hitting a UFO (before explosion)")]
    public int contactDamage = 1;

    [Header("Audio")]
    [Tooltip("Sound when bomb is launched")]
    public AudioClip launchSound;

    [Tooltip("Sound when bomb sticks to surface")]
    public AudioClip stickSound;

    [Tooltip("Ticking sound while armed (optional)")]
    public AudioClip tickSound;

    [Tooltip("Explosion sound")]
    public AudioClip explosionSound;

    private Rigidbody rb;
    private AudioSource audioSource;
    private GameObject owner;
    private bool isStuck = false;
    private float timer = 0f;
    private bool hasExploded = false;
    // private GameObject stuckToUFO = null; // Reserved for future use (tracking stuck target)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Set up rigidbody for projectile motion
        rb.useGravity = false;
        rb.drag = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Freeze rotation to prevent deformation/rolling on surfaces
        rb.freezeRotation = true;

        // Launch forward
        rb.velocity = transform.forward * speed;

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;

        // Play launch sound
        if (launchSound != null)
        {
            audioSource.PlayOneShot(launchSound);
        }

        // Start timer
        timer = fuseTime;
    }

    void Update()
    {
        // Count down timer
        timer -= Time.deltaTime;

        // Explode when timer reaches zero
        if (timer <= 0f && !hasExploded)
        {
            Explode();
        }

        // Play tick sound periodically when timer is low (last 3 seconds)
        if (tickSound != null && timer <= 3f && timer > 0f && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(tickSound);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Don't collide with owner
        if (owner != null && collision.gameObject == owner)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        // Check if hit a UFO
        if (collision.gameObject.CompareTag("Player"))
        {
            // Deal contact damage but don't explode, with kill attribution
            UFOHealth health = collision.gameObject.GetComponent<UFOHealth>();
            if (health != null)
            {
                health.TakeDamage(contactDamage, owner, "Sticky Bomb"); // Pass owner and weapon name
            }

            Debug.Log($"[STICKY BOMB] Hit UFO {collision.gameObject.name} for {contactDamage} contact damage");

            // Trigger wobble effect
            UFOHitEffect hitEffect = collision.gameObject.GetComponent<UFOHitEffect>();
            if (hitEffect != null)
            {
                hitEffect.TriggerWobble();
            }

            // Continue flying - don't stick to UFOs
            return;
        }

        // Hit environment (wall, floor, etc.) - stick to it
        if (!isStuck)
        {
            StickToSurface(collision);
        }
    }

    void StickToSurface(Collision collision)
    {
        isStuck = true;

        // Get the collision normal (direction away from wall)
        Vector3 contactNormal = collision.contacts[0].normal;

        // Get bomb radius from collider
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        float bombRadius = sphereCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);

        // Position bomb so it sits on the surface
        transform.position = collision.contacts[0].point + (contactNormal * bombRadius);

        // Stop all movement
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Disable collider to prevent physics interference
        sphereCollider.enabled = false;

        // DON'T parent - just leave it floating in world space to preserve scale
        // (Arena walls are static anyway, so no need to move with them)

        // Play stick sound
        if (stickSound != null)
        {
            audioSource.PlayOneShot(stickSound);
        }

        Debug.Log($"[STICKY BOMB] Stuck to {collision.gameObject.name}, exploding in {timer:F1}s");
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Debug.Log($"[STICKY BOMB] EXPLODING at {transform.position} with radius {blastRadius}");

        // Find all UFOs in blast radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                // Deal flat damage to everyone in radius (no falloff), with kill attribution
                UFOHealth health = hit.GetComponent<UFOHealth>();
                if (health != null)
                {
                    health.TakeDamage(explosionDamage, owner, "Sticky Bomb Explosion"); // Pass owner and weapon name
                }

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                Debug.Log($"[STICKY BOMB] Explosion damaged {hit.name} for {explosionDamage} damage (distance: {distance:F1})");

                // Trigger wobble effect (distance-based intensity for visual feedback)
                UFOHitEffect hitEffect = hit.GetComponent<UFOHitEffect>();
                if (hitEffect != null)
                {
                    float damageFalloff = 1f - (distance / blastRadius);
                    float wobbleIntensity = 20f * damageFalloff; // Stronger wobble when closer to explosion
                    hitEffect.TriggerWobble(wobbleIntensity);
                }

                // Apply explosion force to UFO (physics knockback)
                Rigidbody targetRb = hit.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    Vector3 explosionDirection = (hit.transform.position - transform.position).normalized;
                    float damageFalloff = 1f - (distance / blastRadius);
                    float explosionForce = 50f * damageFalloff; // Light knockback
                    targetRb.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);
                }
            }
        }

        // Spawn explosion visual effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 5f); // Clean up after 5 seconds
        }

        // Play explosion sound at this position (survives after bomb is destroyed)
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
        }

        // Destroy the bomb
        Destroy(gameObject);
    }

    /// <summary>
    /// Set the owner of this projectile (to prevent self-hits)
    /// </summary>
    public void SetOwner(GameObject ownerObject)
    {
        owner = ownerObject;

        // Ignore collision with owner
        if (owner != null)
        {
            Collider ownerCollider = owner.GetComponent<Collider>();
            Collider bombCollider = GetComponent<Collider>();
            if (ownerCollider != null && bombCollider != null)
            {
                Physics.IgnoreCollision(ownerCollider, bombCollider);
            }
        }
    }

    // Visualize blast radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
