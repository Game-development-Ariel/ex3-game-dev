using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Changes the post-processing (the filter over the whole picture) with the hero's feelings:
/// - the dark edges of the screen slowly grow and shrink, like breathing;
/// - near danger the edges close in and the colors fade;
/// - with less hope the picture is darker (from the design: "more hope, a bit more light");
/// - when caught, the colors drain away for a moment and the picture splits a little.
/// </summary>
[RequireComponent(typeof(Volume))]
public class MoodVolume : MonoBehaviour
{
    [Tooltip("Seconds for one slow breath of the screen's edges")]
    [SerializeField] private float breathSeconds = 5.2f;

    [SerializeField] private float baseVignette = 0.36f;
    [SerializeField] private float dangerVignette = 0.18f;
    [SerializeField] private float baseSaturation = -22f;
    [SerializeField] private float dangerSaturation = -25f;

    [Tooltip("How much darker the picture is at the lowest hope, in exposure stops")]
    [SerializeField] private float lowHopeDarkness = 0.7f;

    private Vignette vignette;
    private ColorAdjustments colors;
    private ChromaticAberration split;

    // 1 right after being caught, then back to 0.
    private float shock;

    private void Start()
    {
        // .profile (and not .sharedProfile) gives this scene its own copy, so the asset on disk doesn't change.
        VolumeProfile profile = GetComponent<Volume>().profile;
        profile.TryGet(out vignette);
        profile.TryGet(out colors);
        profile.TryGet(out split);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Captured += OnCaptured;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Captured -= OnCaptured;
        }
    }

    private void OnCaptured()
    {
        StartCoroutine(Shock());
    }

    private IEnumerator Shock()
    {
        for (shock = 1f; shock > 0f; shock -= Time.deltaTime / 2.5f)
        {
            yield return null;
        }
        shock = 0f;
    }

    private void Update()
    {
        float danger = Tension.Danger;
        float breath = Mathf.Sin(Time.time * 2f * Mathf.PI / breathSeconds) * 0.03f;

        if (vignette != null)
        {
            vignette.intensity.value = baseVignette + breath + danger * dangerVignette + shock * 0.2f;
        }

        if (colors != null)
        {
            float hope = 1f;
            GameManager game = GameManager.Instance;
            if (game != null)
            {
                hope = (float)game.Health / game.MaxHealth;
            }
            colors.saturation.value = baseSaturation + danger * dangerSaturation - shock * 70f;
            colors.postExposure.value = -(1f - hope) * lowHopeDarkness - shock * 0.6f;
        }

        if (split != null)
        {
            split.intensity.value = shock * 0.8f;
        }
    }
}
