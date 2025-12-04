using UnityEngine;

/// <summary>
/// Handles visual feedback when UFO is hit by weapons
/// Creates a wobble/shake effect and brief stun moment
/// </summary>
public class UFOHitEffect : MonoBehaviour
{
    [Header("Wobble Settings")]
    [Tooltip("How much the UFO wobbles when hit (degrees)")]
    public float wobbleAmount = 0f;  // Disabled to eliminate vibration

    [Tooltip("How long the wobble lasts (seconds)")]
    public float wobbleDuration = 1.2f;

    [Tooltip("How quickly the wobble oscillates")]
    public float wobbleSpeed = 14f;

    [Header("Stun Settings")]
    [Tooltip("How long the UFO is stunned when hit (cannot move/shoot)")]
    public float stunDuration = 0.3f;

    [Tooltip("How much the UFO slows down when stunned (0 = full stop, 1 = no slowdown)")]
    public float stunSpeedMultiplier = 0.2f;

    private bool isWobbling = false;
    private bool isStunned = false;
    private float wobbleTimer = 0f;
    private float stunTimer = 0f;
    private float wobbleIntensity = 0f;
    private Vector3 wobbleOffset = Vector3.zero;

    void Update()
    {
        // Update wobble effect
        if (isWobbling)
        {
            wobbleTimer += Time.deltaTime;
            float progress = wobbleTimer / wobbleDuration;

            if (progress >= 1f)
            {
                // Wobble finished
                isWobbling = false;
                wobbleIntensity = 0f;
                wobbleOffset = Vector3.zero;
            }
            else
            {
                // Decay wobble intensity over time (starts strong, fades out)
                wobbleIntensity = wobbleAmount * (1f - progress);

                // Calculate wobble offset (oscillating rotation on multiple axes)
                float wobbleX = Mathf.Sin(Time.time * wobbleSpeed) * wobbleIntensity;
                float wobbleZ = Mathf.Cos(Time.time * wobbleSpeed * 1.3f) * wobbleIntensity;
                wobbleOffset = new Vector3(wobbleX, 0f, wobbleZ);
            }
        }
        else
        {
            wobbleOffset = Vector3.zero;
        }

        // Update stun effect
        if (isStunned)
        {
            stunTimer += Time.deltaTime;
            if (stunTimer >= stunDuration)
            {
                isStunned = false;
            }
        }
    }

    /// <summary>
    /// Get the current wobble rotation offset
    /// Should be called by UFOController and added to banking/pitch rotation
    /// </summary>
    public Vector3 GetWobbleOffset()
    {
        return wobbleOffset;
    }

    /// <summary>
    /// Check if currently wobbling
    /// </summary>
    public bool IsWobbling()
    {
        return isWobbling;
    }

    /// <summary>
    /// Check if currently stunned (UFO cannot move/shoot)
    /// </summary>
    public bool IsStunned()
    {
        return isStunned;
    }

    /// <summary>
    /// Get speed multiplier during stun (for slowing down movement)
    /// </summary>
    public float GetStunSpeedMultiplier()
    {
        return isStunned ? stunSpeedMultiplier : 1f;
    }

    /// <summary>
    /// Trigger wobble effect when hit by weapon
    /// Call this from projectiles or other damage sources
    /// </summary>
    public void TriggerWobble()
    {
        isWobbling = true;
        wobbleTimer = 0f;
        wobbleIntensity = wobbleAmount;

        isStunned = true;
        stunTimer = 0f;
    }

    /// <summary>
    /// Trigger wobble with custom intensity
    /// </summary>
    public void TriggerWobble(float customAmount)
    {
        isWobbling = true;
        wobbleTimer = 0f;
        wobbleIntensity = customAmount;

        isStunned = true;
        stunTimer = 0f;
    }
}
