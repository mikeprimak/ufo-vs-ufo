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

    [Tooltip("Damage dealt on hit")]
    public int damage = 1;

    [Header("Visual Settings")]
    [Tooltip("Optional trail renderer reference")]
    public TrailRenderer trail;

    private Rigidbody rb;
    private float spawnTime;
    private GameObject owner; // Who fired this projectile

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
        // Destroy projectile after lifetime expires
        if (Time.time >= spawnTime + lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Don't hit the UFO that fired this projectile
        if (collision.gameObject == owner)
            return;

        // Check what we hit
        if (collision.gameObject.CompareTag("Player"))
        {
            // Hit another UFO - deal damage
            UFOHealth health = collision.gameObject.GetComponent<UFOHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            // Trigger wobble effect
            UFOHitEffect hitEffect = collision.gameObject.GetComponent<UFOHitEffect>();
            if (hitEffect != null)
            {
                hitEffect.TriggerWobble();
            }
        }

        // Destroy projectile on any collision (wall, floor, player)
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
