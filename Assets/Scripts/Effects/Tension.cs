using UnityEngine;

/// <summary>
/// How much danger the hero feels, from 0 (safe) to 1 (right next to the camera).
/// It grows as the player gets closer to a security camera. The heartbeat and the breathing
/// get louder with it, and MoodVolume darkens the edges of the screen, so the player feels the danger
/// before seeing it.
/// </summary>
public class Tension : MonoBehaviour
{
    /// <summary>The danger now, for other scripts (like MoodVolume) to read.</summary>
    public static float Danger { get; private set; }

    [Tooltip("Closer than this to a camera, in tiles, the danger is full")]
    [SerializeField] private float fullDangerDistance = 6f;

    [Tooltip("Farther than this from every camera, in tiles, there is no danger")]
    [SerializeField] private float noDangerDistance = 14f;

    [Tooltip("How fast the feeling changes, so it rises and calms down smoothly")]
    [SerializeField] private float smoothing = 1.5f;

    [SerializeField] private AudioSource heartbeat;
    [SerializeField] private AudioSource breathing;

    [SerializeField] private float maxHeartbeatVolume = 0.7f;
    [SerializeField] private Vector2 breathingVolume = new Vector2(0.18f, 0.45f);

    private Transform player;
    private SecurityCamera[] cameras;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>().transform;
        cameras = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
        Danger = 0f;
    }

    private void Update()
    {
        float closest = float.MaxValue;
        foreach (SecurityCamera securityCamera in cameras)
        {
            closest = Mathf.Min(closest, Vector2.Distance(player.position, securityCamera.transform.position));
        }

        // InverseLerp: 0 at noDangerDistance, 1 at fullDangerDistance.
        float goal = Mathf.InverseLerp(noDangerDistance, fullDangerDistance, closest);
        Danger = Mathf.MoveTowards(Danger, goal, smoothing * Time.deltaTime);

        heartbeat.volume = Danger * maxHeartbeatVolume;
        heartbeat.pitch = 0.9f + Danger * 0.3f; // A faster heart when the danger is closer.
        breathing.volume = Mathf.Lerp(breathingVolume.x, breathingVolume.y, Danger);
        breathing.pitch = 1f + Danger * 0.25f;
    }
}
