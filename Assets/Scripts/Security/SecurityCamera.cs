using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A security camera on the ceiling that sweeps from side to side.
/// Its yellow light on the floor shows where it is looking.
/// If the player stands in that light and no wall is in the way, the player is caught
/// and sent back to the start point (in the tutorial nobody dies).
/// </summary>
[RequireComponent(typeof(Light2D))]
public class SecurityCamera : MonoBehaviour
{
    [Header("Sweep")]
    [Tooltip("The direction in the middle of the sweep, in degrees. 0 looks up, 90 left, 180 down")]
    [SerializeField] private float centerAngle = 180f;

    [Tooltip("How far the camera turns to each side of the center, in degrees")]
    [SerializeField] private float sweepAngle = 60f;

    [Tooltip("Seconds it takes to turn from one side to the other")]
    [SerializeField] private float turnSeconds = 3f;

    [Tooltip("Seconds the camera stays still at each side before turning back")]
    [SerializeField] private float pauseSeconds = 2f;

    [Header("Vision")]
    [Tooltip("How wide the camera sees, in degrees")]
    [SerializeField] private float viewAngle = 40f;

    [Tooltip("How far the camera sees, in world units (tiles)")]
    [SerializeField] private float viewDistance = 14f;

    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color alarmColor = new Color(1f, 0.2f, 0.15f);

    [Tooltip("Seconds the light stays red after catching the player. The camera doesn't look during this time")]
    [SerializeField] private float alarmSeconds = 1f;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Tooltip("Where the player is sent when caught")]
    [SerializeField] private Transform respawnPoint;

    private Light2D visionLight;

    // Seconds since the sweep started. The angle is calculated from it.
    private float sweepTime;

    // More than 0 while the light is red after a catch.
    private float alarmTimer;

    /// <summary>Keeps the light the same shape as the vision, also while editing values in the Inspector.</summary>
    private void OnValidate()
    {
        ApplyLightShape();
    }

    private void Awake()
    {
        ApplyLightShape();
    }

    private void Update()
    {
        sweepTime += Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, 0f, CurrentAngle());

        if (alarmTimer > 0f)
        {
            alarmTimer -= Time.deltaTime;
            if (alarmTimer <= 0f)
            {
                visionLight.color = normalColor;
            }
            return;
        }

        if (CanSeePlayer())
        {
            CatchPlayer();
        }
    }

    /// <summary>
    /// One full cycle: turn right, pause, turn left, pause.
    /// SmoothStep makes the camera speed up and slow down at the ends, like a real motor.
    /// </summary>
    private float CurrentAngle()
    {
        float cycle = 2f * (turnSeconds + pauseSeconds);
        float t = sweepTime % cycle;

        // -1 is the left end of the sweep and +1 is the right end.
        float side;
        if (t < turnSeconds)
        {
            side = Mathf.Lerp(-1f, 1f, Mathf.SmoothStep(0f, 1f, t / turnSeconds));
        }
        else if (t < turnSeconds + pauseSeconds)
        {
            side = 1f;
        }
        else if (t < 2f * turnSeconds + pauseSeconds)
        {
            side = Mathf.Lerp(1f, -1f, Mathf.SmoothStep(0f, 1f, (t - turnSeconds - pauseSeconds) / turnSeconds));
        }
        else
        {
            side = -1f;
        }

        // Unity angles grow counterclockwise. Looking down, a bigger angle turns the camera to the right.
        return centerAngle + side * sweepAngle;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector2 eye = transform.position;
        Vector2 toPlayer = (Vector2)player.position - eye;

        if (toPlayer.magnitude > viewDistance)
        {
            return false;
        }

        // The camera looks along its "up" direction, like the light cone.
        if (Vector2.Angle(transform.up, toPlayer) > viewAngle / 2f)
        {
            return false;
        }

        // The hits come sorted from the closest. If a wall comes before the player, the wall hides the player.
        RaycastHit2D[] hits = Physics2D.LinecastAll(eye, player.position);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.isTrigger)
            {
                continue;
            }
            return hit.transform == player || hit.transform.IsChildOf(player);
        }
        return true;
    }

    private void CatchPlayer()
    {
        alarmTimer = alarmSeconds;
        visionLight.color = alarmColor;
        Debug.Log("The camera caught the player.");

        if (respawnPoint != null)
        {
            // The player moves with physics, so the Rigidbody2D has to move too, and its speed is reset.
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = respawnPoint.position;
                body.linearVelocity = Vector2.zero;
            }
            player.position = respawnPoint.position;
        }

        // Last, because on the fourth capture it loads the game over scene.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterCapture();
        }
    }

    private void ApplyLightShape()
    {
        if (visionLight == null)
        {
            visionLight = GetComponent<Light2D>();
        }
        if (visionLight == null)
        {
            return;
        }

        // Inner values close to the outer ones give the light sharp edges,
        // so the lit area on the floor is exactly the area where the camera sees the player.
        visionLight.pointLightOuterAngle = viewAngle;
        visionLight.pointLightInnerAngle = viewAngle * 0.9f;
        visionLight.pointLightOuterRadius = viewDistance;
        visionLight.pointLightInnerRadius = viewDistance * 0.85f;
        visionLight.falloffIntensity = 0.2f;
        visionLight.color = normalColor;
    }

    /// <summary>Draws the edges of the vision in the Scene view when the camera is selected.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = normalColor;
        Vector3 left = Quaternion.Euler(0f, 0f, viewAngle / 2f) * transform.up * viewDistance;
        Vector3 right = Quaternion.Euler(0f, 0f, -viewAngle / 2f) * transform.up * viewDistance;
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
    }
}
