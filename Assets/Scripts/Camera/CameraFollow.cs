using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Keeps the camera centered on the target, without showing anything outside the map.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Tooltip("The object to follow, usually the player")]
    [SerializeField] private Transform target;

    [Tooltip("The camera stays inside the area of this tilemap. Leave empty for no limits")]
    [SerializeField] private Tilemap mapBounds;

    [Tooltip("About how many seconds the camera takes to catch up with the target")]
    [SerializeField] private float smoothTime = 0.15f;

    private Camera cam;

    // The area of the map, in world coordinates.
    private Bounds worldBounds;

    // The camera's current speed. SmoothDamp reads and updates it every frame.
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    /// <summary>Finds the area of the map and jumps straight to the target.</summary>
    private void Start()
    {
        if (mapBounds != null)
        {
            mapBounds.CompressBounds(); // Shrinks the bounds to the cells that have tiles.
            worldBounds = mapBounds.localBounds;
            worldBounds.center = mapBounds.transform.TransformPoint(worldBounds.center);
        }

        if (target != null)
        {
            transform.position = GoalPosition();
        }
    }

    /// <summary>
    /// Moves the camera smoothly toward the goal.
    /// LateUpdate runs after the player moved, so the camera doesn't lag a frame behind.
    /// </summary>
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, GoalPosition(), ref velocity, smoothTime);
    }

    /// <summary>Returns where the camera should be: on the target, but inside the map.</summary>
    private Vector3 GoalPosition()
    {
        Vector3 goal = target.position;
        goal.z = transform.position.z; // The camera must stay at its own distance from the map.

        if (mapBounds == null)
        {
            return goal;
        }

        // orthographicSize is half of the height the camera sees, and aspect is width / height.
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        goal.x = ClampAxis(goal.x, worldBounds.min.x, worldBounds.max.x, halfWidth);
        goal.y = ClampAxis(goal.y, worldBounds.min.y, worldBounds.max.y, halfHeight);
        return goal;
    }

    /// <summary>
    /// Limits one axis so the edge of the view never passes the edge of the map.
    /// </summary>
    /// <param name="value">The wanted camera position on this axis.</param>
    /// <param name="min">The low edge of the map.</param>
    /// <param name="max">The high edge of the map.</param>
    /// <param name="halfView">Half of what the camera sees on this axis.</param>
    private static float ClampAxis(float value, float min, float max, float halfView)
    {
        float mapSize = max - min;
        float viewSize = halfView * 2f;

        // If the map is smaller than the view on this axis, just center it.
        if (mapSize < viewSize)
        {
            return (min + max) / 2f;
        }

        return Mathf.Clamp(value, min + halfView, max - halfView);
    }
}
