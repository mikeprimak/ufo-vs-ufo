using UnityEngine;

/// <summary>
/// Simple explosion visual effect that expands briefly then fades out
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("How long the explosion lasts (seconds)")]
    public float duration = 0.33f;

    [Tooltip("Starting scale (should match bomb size)")]
    public float startScale = 12.5f;

    [Tooltip("Ending scale (should match blast radius)")]
    public float endScale = 90f;

    private float timer = 0f;
    private Renderer rend;
    private Material mat;
    private Color startColor;

    void Start()
    {
        // Get the renderer and material
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            startColor = mat.color;
        }

        // Set starting scale
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        if (progress >= 1f)
        {
            // Explosion finished
            Destroy(gameObject);
            return;
        }

        // Expand the sphere
        float currentScale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = Vector3.one * currentScale;

        // Fade out
        if (mat != null)
        {
            Color color = startColor;
            color.a = 1f - progress; // Fade from 1 to 0
            mat.color = color;
        }
    }
}
