using UnityEngine;

/// <summary>
/// Tiny specks of dust that float slowly in the air. The material is lit by the 2D lights,
/// so the dust shows only where there is light, like dust in a beam in a dark room.
/// The particle system is set up here in code, so all its settings are in one readable place.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class DustMotes : MonoBehaviour
{
    [Tooltip("The size of the area the dust floats in, in tiles")]
    [SerializeField] private Vector2 area = new Vector2(8f, 5f);

    [Tooltip("A material lit by the 2D lights")]
    [SerializeField] private Material material;

    [SerializeField] private int maxSpecks = 70;

    private void Awake()
    {
        var particles = GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new Color(1f, 0.95f, 0.85f, 0.55f);
        main.maxParticles = maxSpecks;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true; // The room is already dusty when the scene starts.

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = maxSpecks / 8f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(area.x, area.y, 0.1f);

        // Noise makes each speck drift and turn a little, like in still air.
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.25f;

        // Each speck fades in and out, so none of them pop.
        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var particleRenderer = GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = material;
        particleRenderer.sortingOrder = 15; // In the air, above the player.

        particles.Play();
    }
}
