using UnityEngine;

/// <summary>
/// A security camera on the ceiling that sweeps from side to side.
/// A yellow cone on the floor shows exactly what it sees: the cone is built from rays that stop at the first wall,
/// and the same rays decide whether the player is seen. So if the cone touches the player, the player is caught,
/// and behind a wall there is no cone and no danger.
/// When caught, the player is sent back to the start point and the GameManager counts the capture.
/// </summary>
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

    [Tooltip("How many rays build the cone. More rays = the cone follows the walls more exactly")]
    [SerializeField] private int coneRays = 40;

    [Tooltip("The object that draws the cone. Its mesh is rebuilt every frame")]
    [SerializeField] private MeshFilter visionCone;

    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.3f, 0.35f);
    [SerializeField] private Color alarmColor = new Color(1f, 0.2f, 0.15f, 0.5f);

    [Tooltip("Seconds the cone stays red after catching the player. The camera doesn't look during this time")]
    [SerializeField] private float alarmSeconds = 1f;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Tooltip("Where the player is sent when caught")]
    [SerializeField] private Transform respawnPoint;

    // Seconds since the sweep started. The angle is calculated from it.
    private float sweepTime;

    // More than 0 while the cone is red after a catch.
    private float alarmTimer;

    private Mesh coneMesh;
    private Vector3[] coneVertices;
    private Color[] coneColors;
    private int[] coneTriangles;

    private void Awake()
    {
        CreateConeMesh();
    }

    private void Update()
    {
        sweepTime += Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, 0f, CurrentAngle());

        bool alarm = alarmTimer > 0f;
        if (alarm)
        {
            alarmTimer -= Time.deltaTime;
        }
        else if (CanSeePlayer())
        {
            CatchPlayer();
            alarm = true;
        }

        UpdateCone(alarm ? alarmColor : normalColor);
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
        float distance = toPlayer.magnitude;

        // The camera looks along its "up" direction, like the cone.
        if (distance > viewDistance || Vector2.Angle(transform.up, toPlayer) > viewAngle / 2f)
        {
            return false;
        }

        // Seen only if no wall is closer than the player along the line between them.
        return DistanceToWall(eye, toPlayer / distance, distance) >= distance;
    }

    /// <summary>
    /// How far a ray from the camera goes before it hits a wall, up to maxDistance.
    /// Both the cone and the player check use this, so what the cone shows is exactly what the camera sees.
    /// </summary>
    private float DistanceToWall(Vector2 origin, Vector2 direction, float maxDistance)
    {
        // The hits come sorted from the closest. Triggers (like the bottle) and the player don't block the view.
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, maxDistance);
        foreach (RaycastHit2D hit in hits)
        {
            bool isPlayer = player != null && (hit.transform == player || hit.transform.IsChildOf(player));
            if (!hit.collider.isTrigger && !isPlayer)
            {
                return hit.distance;
            }
        }
        return maxDistance;
    }

    private void CatchPlayer()
    {
        alarmTimer = alarmSeconds;
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

    /// <summary>
    /// The cone is a fan of triangles: vertex 0 is the camera, and the others are the ends of the rays.
    /// The triangles never change, only where the ray ends are, so they are built once here.
    /// </summary>
    private void CreateConeMesh()
    {
        if (visionCone == null)
        {
            return;
        }

        coneMesh = new Mesh { name = "Vision Cone" };
        coneMesh.MarkDynamic(); // Tells Unity the mesh changes every frame.
        visionCone.mesh = coneMesh;

        int rayCount = Mathf.Max(2, coneRays);
        coneVertices = new Vector3[rayCount + 1];
        coneColors = new Color[rayCount + 1];
        coneTriangles = new int[(rayCount - 1) * 3];
        for (int i = 0; i < rayCount - 1; i++)
        {
            coneTriangles[i * 3] = 0;
            coneTriangles[i * 3 + 1] = i + 1;
            coneTriangles[i * 3 + 2] = i + 2;
        }
    }

    private void UpdateCone(Color color)
    {
        if (coneMesh == null)
        {
            return;
        }

        Vector2 eye = transform.position;
        int rayCount = coneVertices.Length - 1;
        coneVertices[0] = Vector3.zero;

        for (int i = 0; i < rayCount; i++)
        {
            // From the left edge of the view to the right edge, in the camera's own (local) directions.
            float angle = Mathf.Lerp(viewAngle / 2f, -viewAngle / 2f, i / (rayCount - 1f));
            Vector3 localDirection = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            Vector2 worldDirection = transform.TransformDirection(localDirection);
            coneVertices[i + 1] = localDirection * DistanceToWall(eye, worldDirection, viewDistance);
        }

        for (int i = 0; i < coneColors.Length; i++)
        {
            coneColors[i] = color;
        }

        coneMesh.vertices = coneVertices;
        coneMesh.colors = coneColors;
        coneMesh.triangles = coneTriangles;
        coneMesh.RecalculateBounds();
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
