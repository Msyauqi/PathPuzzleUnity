using UnityEngine;

public class PlacementEffect : MonoBehaviour
{
    public float duration = 1f;

    [Header("Animation")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Material material;
    private float timer = 0f;
    private Vector3 initialScale;

    void Start()
    {
        // 🔥 safety duration
        if (duration <= 0f)
            duration = 0.01f;

        initialScale = transform.localScale;

        Renderer renderer = GetComponent<Renderer>();

        if (renderer != null)
        {
            // 🔥 instance material (hindari ubah sharedMaterial)
            material = renderer.material;

            SetupTransparent(material);
        }

        Destroy(gameObject, duration);
    }

    void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);

        // 🔥 SCALE (respect prefab scale)
        float scale = scaleCurve.Evaluate(t);
        transform.localScale = initialScale * scale;

        // 🔥 FADE
        if (material != null)
        {
            Color color = material.color;
            color.a = alphaCurve.Evaluate(t);
            material.color = color;
        }
    }

    // =========================
    // 🔥 FORCE TRANSPARENT MODE
    // =========================
    void SetupTransparent(Material mat)
    {
        if (mat == null) return;

        // Universal Render Pipeline / Standard fallback
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
    }
}