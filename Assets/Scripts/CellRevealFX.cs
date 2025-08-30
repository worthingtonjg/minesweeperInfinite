using System.Collections;
using UnityEngine;
using TMPro;

public class CellRevealFX : MonoBehaviour
{
    [Header("Timing")]
    public float delay = 0f;
    public float duration = 0.12f;

    [Header("Scale")]
    public float startScaleMult = 0.82f;
    public float overshootMult = 1.06f;
    public float endScale; // should be cellSize

    [Header("Fade")]
    [Range(0f, 1f)] public float startAlpha = 0f;

    SpriteRenderer[] sprites;
    TextMeshPro[] tmps;
    Transform tr;
    bool isConfigured;

    void Awake()
    {
        tr = transform;
    }

    // Call this AFTER you've set delay/duration/endScale and AFTER children (like TMP) exist.
    public void ConfigureAndPlay()
    {
        // (Re)grab children now that hierarchy is complete
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        tmps    = GetComponentsInChildren<TextMeshPro>(true);

        isConfigured = true;
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        if (!isConfigured) yield break;

        // initialize
        SetAlpha(startAlpha);
        float baseScale = endScale <= 0 ? 1f : endScale;
        tr.localScale = Vector3.one * baseScale * startScaleMult;

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            float s = EaseOutBack(u);
            float scale = Mathf.LerpUnclamped(baseScale * startScaleMult,
                                             baseScale * overshootMult, s);
            scale = Mathf.Lerp(scale, baseScale, u * 0.65f);
            tr.localScale = Vector3.one * scale;

            SetAlpha(Mathf.Lerp(startAlpha, 1f, u));
            yield return null;
        }

        tr.localScale = Vector3.one * baseScale;
        SetAlpha(1f);
    }

    static float EaseOutBack(float x)
    {
        const float s = 1.70158f;
        x -= 1f;
        return (x * x * ((s + 1f) * x + s) + 1f);
    }

    void SetAlpha(float a)
    {
        if (sprites != null)
            foreach (var sr in sprites) if (sr) { var c = sr.color; c.a = a; sr.color = c; }

        if (tmps != null)
            foreach (var t in tmps) if (t) { var c = t.color; c.a = a; t.color = c; }
    }
}
