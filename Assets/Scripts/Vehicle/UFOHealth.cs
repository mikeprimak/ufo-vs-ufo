using UnityEngine;

/// <summary>
/// Manages UFO health, damage, and death state
/// Each UFO has 3 HP per round - when HP reaches 0, UFO explodes and becomes a physics wreck
/// </summary>
public class UFOHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health points (default: 3)")]
    public int maxHealth = 3;

    [Header("Death Settings")]
    [Tooltip("Explosion effect prefab to spawn on death (optional)")]
    public GameObject deathExplosionPrefab;

    [Tooltip("How long the wreck stays on screen before cleanup (seconds)")]
    public float wreckLifetime = 10f;

    [Header("Audio")]
    [Tooltip("Sound to play when UFO dies/explodes")]
    public AudioClip deathSound;

    // Current health
    private int currentHealth;

    // Is this UFO dead?
    private bool isDead = false;

    // Components
    private Rigidbody rb;
    private UFOController controller;
    private UFOCollision collision;

    void Start()
    {
        // Initialize health to max
        currentHealth = maxHealth;

        // Get components
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<UFOController>();
        collision = GetComponent<UFOCollision>();
    }

    /// <summary>
    /// Apply damage to this UFO
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isDead)
        {
            Debug.Log($"[UFO HEALTH] {gameObject.name} is already dead, ignoring {damageAmount} damage");
            return; // Already dead, ignore further damage
        }

        // Reduce health
        int oldHealth = currentHealth;
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth); // Clamp to 0

        Debug.Log($"[UFO HEALTH] {gameObject.name} took {damageAmount} damage. Health: {oldHealth} → {currentHealth}/{maxHealth}");

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Kill this UFO - spawn explosion, disable controls, enable gravity physics
    /// </summary>
    void Die()
    {
        if (isDead)
            return; // Already dead

        isDead = true;

        Debug.Log($"[UFO HEALTH] {gameObject.name} has been destroyed!");

        // Spawn death explosion effect
        if (deathExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 5f); // Clean up explosion after 5 seconds
        }

        // Play death sound
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 1f);
        }

        // Disable flight controls
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Disable collision bounce behavior (let it be a passive physics object)
        if (collision != null)
        {
            collision.enabled = false;
        }

        // Enable gravity and let it fall
        if (rb != null)
        {
            rb.useGravity = true;
            rb.drag = 0.5f; // Add some air resistance
            rb.angularDrag = 0.5f; // Add rotational drag

            // Unfreeze rotation so it can tumble
            rb.constraints = RigidbodyConstraints.None;

            // Add a small upward impulse for dramatic effect
            rb.AddForce(Vector3.up * 3f, ForceMode.Impulse);

            // Add random spin
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }

        // Set wreck to cleanup after timeout
        Destroy(gameObject, wreckLifetime);
    }

    /// <summary>
    /// Get current health
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Get max health
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Check if UFO is dead
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }

    /// <summary>
    /// Heal the UFO (for power-ups, respawns, etc.)
    /// </summary>
    public void Heal(int healAmount)
    {
        if (isDead)
            return;

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Clamp to max

        Debug.Log($"[UFO HEALTH] {gameObject.name} healed {healAmount}. Health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Reset health to full (for respawns, new rounds)
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        Debug.Log($"[UFO HEALTH] {gameObject.name} health reset to {maxHealth}");
    }
}
