using UnityEngine;

/// <summary>
/// Adds subtle hover wobble and bob to the UFO visual model
/// Makes the UFO feel alive and floating
/// </summary>
public class UFOHoverWobble : MonoBehaviour
{
    [Header("Wobble Settings")]
    [Tooltip("Visual model to wobble (usually a child object with the mesh)")]
    public Transform visualModel;

    [Tooltip("How much the UFO bobs up and down")]
    public float bobAmount = 0.1f;

    [Tooltip("Speed of bobbing")]
    public float bobSpeed = 1.5f;

    [Tooltip("How much the UFO wobbles side to side")]
    public float wobbleAmount = 0.5f;

    [Tooltip("Speed of wobbling")]
    public float wobbleSpeed = 2f;

    [Tooltip("Random variation in wobble (makes it less uniform)")]
    public float randomness = 0.3f;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private float randomOffset;

    void Start()
    {
        if (visualModel == null)
        {
            Debug.LogWarning("UFOHoverWobble: No visual model assigned. Assign the UFO mesh object in the inspector.");
            enabled = false;
            return;
        }

        // Store original local transform
        originalLocalPosition = visualModel.localPosition;
        originalLocalRotation = visualModel.localRotation;

        // Random offset so multiple UFOs don't wobble in sync
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (visualModel == null)
            return;

        float time = Time.time + randomOffset;

        // Calculate bob (up and down)
        float bob = Mathf.Sin(time * bobSpeed) * bobAmount;

        // Calculate wobble (slight rotation)
        float wobbleX = Mathf.Sin(time * wobbleSpeed) * wobbleAmount;
        float wobbleZ = Mathf.Cos(time * wobbleSpeed * 0.7f) * wobbleAmount;

        // Add some randomness
        float randomBob = Mathf.PerlinNoise(time * 0.5f, 0) * bobAmount * randomness;
        float randomWobble = Mathf.PerlinNoise(0, time * 0.3f) * wobbleAmount * randomness;

        // Apply position offset (bob)
        Vector3 newPosition = originalLocalPosition + Vector3.up * (bob + randomBob);
        visualModel.localPosition = newPosition;

        // Apply rotation offset (wobble)
        Quaternion wobbleRotation = Quaternion.Euler(wobbleX + randomWobble, 0, wobbleZ);

        // Combine original rotation with wobble (preserve banking from UFOController)
        visualModel.localRotation = visualModel.localRotation * wobbleRotation;
    }
}
