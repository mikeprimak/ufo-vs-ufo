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

    [Tooltip("Invincibility duration after taking damage (seconds)")]
    public float invincibilityDuration = 3f;

    [Header("Invincibility Visual Feedback")]
    [Tooltip("Enable visual blink/flash during invincibility frames")]
    public bool enableInvincibilityBlink = true;

    [Tooltip("How fast the UFO blinks during invincibility (blinks per second)")]
    public float blinkFrequency = 8f;

    [Tooltip("Renderer to flash (usually UFO_Body)")]
    public Renderer ufoRenderer;

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

    // Invincibility frames
    private bool isInvincible = false;
    private float invincibilityEndTime = 0f;

    // Blink effect
    private bool isVisible = true;
    private float nextBlinkTime = 0f;

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

    void Update()
    {
        // Update invincibility frames
        if (isInvincible)
        {
            // Handle blinking effect during invincibility
            if (enableInvincibilityBlink && ufoRenderer != null)
            {
                // Toggle visibility at blink frequency
                if (Time.time >= nextBlinkTime)
                {
                    isVisible = !isVisible;
                    ufoRenderer.enabled = isVisible;
                    nextBlinkTime = Time.time + (1f / blinkFrequency);
                }
            }

            // Check if invincibility expired
            if (Time.time >= invincibilityEndTime)
            {
                isInvincible = false;

                Debug.Log($"[UFO HEALTH] {gameObject.name} invincibility expired at {Time.time}");

                // Ensure renderer is visible when i-frames end
                if (ufoRenderer != null)
                {
                    ufoRenderer.enabled = true;
                    isVisible = true;
                }
            }
        }
    }

    /// <summary>
    /// Apply damage to this UFO
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (isDead)
        {
            return; // Already dead, ignore further damage
        }

        // Check invincibility frames - prevent rapid-fire damage
        if (isInvincible)
        {
            Debug.Log($"[UFO HEALTH] {gameObject.name} is invincible! Damage blocked.");
            return;
        }

        // Reduce health
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth); // Clamp to 0

        Debug.Log($"[UFO HEALTH] {gameObject.name} took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");

        // Activate invincibility frames
        isInvincible = true;
        invincibilityEndTime = Time.time + invincibilityDuration;
        nextBlinkTime = Time.time; // Start blinking immediately

        Debug.Log($"[UFO HEALTH] {gameObject.name} invincibility activated for {invincibilityDuration} seconds (until {invincibilityEndTime})");

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
    /// Check if UFO is currently invincible (i-frames active)
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
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
