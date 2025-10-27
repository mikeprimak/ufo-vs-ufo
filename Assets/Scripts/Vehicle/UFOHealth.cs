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

    [Header("Death Explosion Settings")]
    [Tooltip("Enable large explosion 2 seconds after death")]
    public bool enableGroundExplosion = true;

    [Tooltip("Explosion prefab to spawn at UFO position")]
    public GameObject groundExplosionPrefab;

    [Tooltip("Blast radius for explosion damage")]
    public float groundExplosionRadius = 60f;

    [Tooltip("Damage dealt by explosion")]
    public int groundExplosionDamage = 1;

    [Tooltip("Sound to play on explosion")]
    public AudioClip groundExplosionSound;

    [Header("Audio")]
    [Tooltip("Sound to play when UFO dies/explodes")]
    public AudioClip deathSound;

    // Current health
    private int currentHealth;

    // Is this UFO dead?
    private bool isDead = false;

    // Death explosion state
    private bool hasExplodedOnGround = false;
    private GameObject lastKiller = null; // Track who killed this UFO for explosion attribution
    private float deathTime = 0f; // Time when UFO died (for timer-based explosion)

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

                // Ensure renderer is visible when i-frames end
                if (ufoRenderer != null)
                {
                    ufoRenderer.enabled = true;
                    isVisible = true;
                }
            }
        }

        // Trigger explosion 2 seconds after death
        if (isDead && !hasExplodedOnGround && enableGroundExplosion)
        {
            if (Time.time - deathTime >= 2f)
            {
                hasExplodedOnGround = true;

                // Break apart UFO at the moment of explosion
                BreakApartUFO();

                TriggerGroundExplosion(transform.position);
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
            return;
        }

        // Reduce health
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth); // Clamp to 0

        // Track damage taken in stats
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.RecordDamageTaken(damageAmount);
        }

        // Activate invincibility frames
        isInvincible = true;
        invincibilityEndTime = Time.time + invincibilityDuration;
        nextBlinkTime = Time.time; // Start blinking immediately

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Apply damage to this UFO and track who dealt it (for kill tracking)
    /// </summary>
    public void TakeDamage(int damageAmount, GameObject attacker)
    {
        if (isDead) return;
        if (isInvincible)
        {
            return;
        }

        // Reduce health
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);

        // Track damage in stats
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.RecordDamageTaken(damageAmount);
        }

        // Track damage dealt by attacker
        if (attacker != null)
        {
            PlayerStats attackerStats = attacker.GetComponent<PlayerStats>();
            if (attackerStats != null)
            {
                attackerStats.RecordDamageDealt(damageAmount);
            }

            // Log hit to combat log
            CombatLogUI.LogHit(attacker, gameObject, damageAmount);
        }

        // Activate invincibility frames
        isInvincible = true;
        invincibilityEndTime = Time.time + invincibilityDuration;
        nextBlinkTime = Time.time;

        // Check if dead
        if (currentHealth <= 0)
        {
            Die(attacker);
        }
    }

    /// <summary>
    /// Kill this UFO - spawn explosion, disable controls, enable gravity physics
    /// </summary>
    void Die()
    {
        Die(null);
    }

    /// <summary>
    /// Kill this UFO and track who killed it
    /// </summary>
    void Die(GameObject killer)
    {
        if (isDead)
            return; // Already dead

        isDead = true;
        lastKiller = killer; // Store killer for explosion attribution
        deathTime = Time.time; // Record death time for timer-based explosion

        // Notify GameManager of death
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RecordDeath(gameObject);

            // If killed by another player, record kill for them
            if (killer != null && killer != gameObject)
            {
                gameManager.RecordKill(killer);

                // Log kill to combat log
                CombatLogUI.LogKill(killer, gameObject);
            }
            else
            {
                // Log suicide/environmental death
                CombatLogUI.LogSuicide(gameObject);
            }
        }

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

        // Don't break apart yet - wait until the explosion at 2 seconds

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
            rb.drag = 2f; // More air resistance for slower, controlled fall
            rb.angularDrag = 3f; // Heavy rotational drag to reduce spinning

            // Unfreeze rotation so it can tumble slightly
            rb.constraints = RigidbodyConstraints.None;

            // NO upward impulse - just let it drop naturally
            // Kill most of the current velocity for a clean death drop
            rb.velocity = rb.velocity * 0.3f; // Keep 30% of momentum

            // Very gentle random tumble (not crazy spinning)
            Vector3 gentleTumble = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-1f, 1f)
            );
            rb.AddTorque(gentleTumble, ForceMode.Impulse);
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
    }

    /// <summary>
    /// Reset health to full (for respawns, new rounds)
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        hasExplodedOnGround = false;
        lastKiller = null;
    }

    /// <summary>
    /// Called when UFO collides with something
    /// NOTE: Death explosion uses timer-based triggering in Update() instead of collision detection
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        // Death explosion is handled by timer in Update() method
    }

    /// <summary>
    /// Trigger large explosion with area damage at UFO position
    /// </summary>
    void TriggerGroundExplosion(Vector3 explosionPoint)
    {
        // Spawn explosion visual effect
        if (groundExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(groundExplosionPrefab, explosionPoint, Quaternion.identity);
            // Scale up the explosion to match the large blast radius (60 units / 20 = 3x scale)
            float scale = groundExplosionRadius / 20f;
            explosion.transform.localScale = Vector3.one * scale;
            Destroy(explosion, 5f);
        }
        else
        {
            // Create a fallback visual if no prefab assigned
            GameObject fallbackSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallbackSphere.name = "GroundExplosion_Fallback";
            fallbackSphere.transform.position = explosionPoint;
            fallbackSphere.transform.localScale = Vector3.one * groundExplosionRadius * 2f; // Diameter = 120 units

            // Make it bright and visible
            Renderer renderer = fallbackSphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange, semi-transparent
                renderer.material.SetFloat("_Mode", 3); // Transparent mode
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }

            // Remove collider so it doesn't interfere
            Collider col = fallbackSphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Destroy(fallbackSphere, 1f);
        }

        // Play ground explosion sound
        if (groundExplosionSound != null)
        {
            AudioSource.PlayClipAtPoint(groundExplosionSound, explosionPoint, 1.5f); // Louder for dramatic effect
        }

        // Deal damage to all UFOs in blast radius
        Collider[] hitColliders = Physics.OverlapSphere(explosionPoint, groundExplosionRadius);
        System.Collections.Generic.HashSet<GameObject> damagedUFOs = new System.Collections.Generic.HashSet<GameObject>();

        foreach (Collider hit in hitColliders)
        {
            // Check if we hit a UFO (root object with Player tag)
            if (hit.transform.root.CompareTag("Player"))
            {
                GameObject rootUFO = hit.transform.root.gameObject;

                // Skip this UFO (the one that exploded)
                if (rootUFO == gameObject)
                    continue;

                // Skip if already damaged this UFO
                if (damagedUFOs.Contains(rootUFO))
                    continue;

                damagedUFOs.Add(rootUFO);

                // Deal damage with attribution to the killer (if any)
                UFOHealth health = rootUFO.GetComponent<UFOHealth>();
                if (health != null)
                {
                    if (lastKiller != null)
                    {
                        health.TakeDamage(groundExplosionDamage, lastKiller);
                    }
                    else
                    {
                        health.TakeDamage(groundExplosionDamage);
                    }
                }

                // Apply massive knockback force
                Rigidbody targetRb = rootUFO.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    Vector3 explosionDirection = (rootUFO.transform.position - explosionPoint).normalized;
                    float distance = Vector3.Distance(explosionPoint, rootUFO.transform.position);
                    float damageFalloff = 1f - (distance / groundExplosionRadius);
                    float explosionForce = 80f * damageFalloff; // Much stronger than regular explosion
                    targetRb.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);
                }
            }
        }
    }

    /// <summary>
    /// Break apart UFO gently at explosion - dome and body separate and fall apart meekly
    /// </summary>
    void BreakApartUFO()
    {
        // Find UFO_Visual container (contains UFO_Body and UFO_Dome)
        Transform ufoVisual = transform.Find("UFO_Visual");
        if (ufoVisual == null)
            return;

        // Find the dome and body
        Transform dome = ufoVisual.Find("UFO_Dome");
        Transform body = ufoVisual.Find("UFO_Body");
        if (dome == null)
            return;

        // Detach dome from parent (make it independent)
        dome.SetParent(null);

        // Add Rigidbody to dome so it becomes a physics object
        Rigidbody domeRb = dome.gameObject.AddComponent<Rigidbody>();
        domeRb.mass = 0.3f; // Very light
        domeRb.drag = 2f; // High air resistance so it doesn't fly far
        domeRb.angularDrag = 1f; // Some rotational drag
        domeRb.useGravity = true;

        // Add sphere collider to dome
        SphereCollider domeCollider = dome.gameObject.AddComponent<SphereCollider>();
        domeCollider.radius = 0.6f;

        // Gentle separation forces - small upward and sideways drift
        Vector3 gentleSeparation = new Vector3(
            Random.Range(-0.5f, 0.5f),  // Small horizontal X
            Random.Range(0.5f, 1f),      // Small upward lift
            Random.Range(-0.5f, 0.5f)    // Small horizontal Z
        );
        domeRb.AddForce(gentleSeparation * 3f, ForceMode.Impulse); // Gentle force

        // Gentle lazy tumble for dome
        Vector3 gentleDomeSpin = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        );
        domeRb.AddTorque(gentleDomeSpin, ForceMode.Impulse);

        // Give body a small push in opposite direction (so they separate slightly)
        if (rb != null)
        {
            Vector3 bodyPush = -gentleSeparation * 0.5f; // Half the dome's force, opposite direction
            rb.AddForce(bodyPush * 3f, ForceMode.Impulse);
        }

        // Clean up dome after wreck lifetime (same as body)
        Destroy(dome.gameObject, wreckLifetime);
    }
}
