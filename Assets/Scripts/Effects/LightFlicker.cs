using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Makes a light bulb flicker like a weak bulb on a tired generator:
/// a small restless shimmer all the time, and sometimes a short dip almost to darkness.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Tooltip("How strong the constant shimmer is. 0.1 = 10 percent")]
    [SerializeField] private float shimmer = 0.12f;

    [Tooltip("On average, seconds between two dips")]
    [SerializeField] private float secondsBetweenDips = 7f;

    private Light2D bulb;
    private float baseIntensity;
    private float seed;
    private float dipTimer;

    private void Start()
    {
        bulb = GetComponent<Light2D>();
        baseIntensity = bulb.intensity;
        seed = Random.value * 100f; // Each bulb flickers differently.
        dipTimer = Random.Range(2f, secondsBetweenDips * 2f);
    }

    private void Update()
    {
        // Perlin noise changes smoothly, so the shimmer looks natural and not random jumps.
        float noise = Mathf.PerlinNoise(seed, Time.time * 3f) - 0.5f;
        float intensity = baseIntensity * (1f + noise * 2f * shimmer);

        dipTimer -= Time.deltaTime;
        if (dipTimer < 0f)
        {
            intensity *= 0.15f; // The dip lasts a few frames.
            if (dipTimer < -0.12f)
            {
                dipTimer = Random.Range(secondsBetweenDips * 0.5f, secondsBetweenDips * 1.5f);
            }
        }
        bulb.intensity = intensity;
    }
}
