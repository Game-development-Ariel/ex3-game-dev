using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player where to go: a faint light-colored chevron floats over the current goal
/// (the water bottle, and after picking it up, the mattress).
/// When the goal is off the screen, a second arrow on the screen's edge points toward it.
/// </summary>
public class ObjectiveMarker : MonoBehaviour
{
    // Above the HUD canvas's sorting order is not needed: the edge arrow has its own canvas.
    private const int CanvasSortingOrder = 12;

    private static readonly Color MarkerColor = new Color(1f, 0.95f, 0.85f, 0.75f);

    // The chevron sprite is drawn pointing down. Rotating by (angle + 90) points it at the angle.
    private const float SpriteDownOffset = 90f;

    [Tooltip("Where to go after the bottle is picked up")]
    [SerializeField] private Transform mattress;

    [Tooltip("How high over the goal the arrow floats, in world units")]
    [SerializeField] private float heightAbove = 1f;

    [Tooltip("The size of the floating arrow in the world")]
    [SerializeField] private float worldArrowScale = 0.8f;

    [Tooltip("Distance of the edge arrow from the screen's edge, in UI pixels")]
    [SerializeField] private float edgeMargin = 90f;

    private Camera cam;
    private Transform worldArrow;
    private RectTransform canvasRect;
    private RectTransform edgeArrow;
    private WaterBottle bottle;

    private void Start()
    {
        cam = Camera.main;
        CreateWorldArrow();

        Canvas canvas = UiFactory.CreateCanvas("Objective Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        canvasRect = canvas.GetComponent<RectTransform>();
        Image arrow = UiFactory.CreateIcon(canvas.transform, "Chevron", new Vector2(60f, 60f));
        arrow.color = MarkerColor;
        edgeArrow = arrow.rectTransform;
        UiFactory.Place(edgeArrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
    }

    /// <summary>
    /// The floating arrow: a parent that follows the goal, and a child sprite that bobs.
    /// The bob moves the child, so following and bobbing don't fight each other.
    /// </summary>
    private void CreateWorldArrow()
    {
        worldArrow = new GameObject("Objective Arrow").transform;
        worldArrow.SetParent(transform, false);

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(worldArrow, false);
        sprite.transform.localScale = Vector3.one * worldArrowScale;
        SpriteRenderer spriteRenderer = sprite.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = UiTheme.Sprite("Chevron");
        spriteRenderer.color = MarkerColor;
        spriteRenderer.sortingOrder = 20; // Above the walls and the player.
        sprite.AddComponent<Bob>();

        // A faint glow behind the chevron, so it reads in the dark without shouting.
        var glow = new GameObject("Glow");
        glow.transform.SetParent(sprite.transform, false);
        glow.transform.localScale = Vector3.one * 1.6f;
        SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = UiTheme.Sprite("Glow");
        glowRenderer.color = new Color(1f, 0.85f, 0.6f, 0.25f);
        glowRenderer.sortingOrder = 19;
    }

    // LateUpdate: after the camera and the player moved this frame.
    private void LateUpdate()
    {
        Transform goal = CurrentGoal();
        worldArrow.gameObject.SetActive(goal != null);
        edgeArrow.gameObject.SetActive(false);
        if (goal == null)
        {
            return;
        }

        worldArrow.position = goal.position + Vector3.up * heightAbove;

        // Viewport: (0,0) is the bottom left of the screen and (1,1) the top right.
        Vector3 viewport = cam.WorldToViewportPoint(goal.position);
        bool onScreen = viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
        if (!onScreen)
        {
            PlaceEdgeArrow(viewport);
        }
    }

    private Transform CurrentGoal()
    {
        GameManager game = GameManager.Instance;
        if (game != null && game.HasBottle)
        {
            return mattress;
        }

        // The spawner makes a new bottle after the player drops it, so we look for it again when it's gone.
        if (bottle == null)
        {
            bottle = FindAnyObjectByType<WaterBottle>();
        }
        return bottle != null ? bottle.transform : null;
    }

    /// <summary>Puts the edge arrow on the line from the screen's center toward the goal, just inside the edge.</summary>
    private void PlaceEdgeArrow(Vector3 viewport)
    {
        Vector2 half = canvasRect.rect.size / 2f;
        Vector2 direction = new Vector2((viewport.x - 0.5f) * half.x * 2f, (viewport.y - 0.5f) * half.y * 2f);

        // Stretch the direction until it touches the nearer edge (left/right or top/bottom).
        float toSide = (half.x - edgeMargin) / Mathf.Max(Mathf.Abs(direction.x), 0.001f);
        float toTop = (half.y - edgeMargin) / Mathf.Max(Mathf.Abs(direction.y), 0.001f);
        Vector2 position = direction * Mathf.Min(toSide, toTop);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float pulse = 1f + Mathf.Sin(Time.time * 2.5f) * 0.06f;

        edgeArrow.gameObject.SetActive(true);
        edgeArrow.anchoredPosition = position;
        edgeArrow.localRotation = Quaternion.Euler(0f, 0f, angle + SpriteDownOffset);
        edgeArrow.localScale = Vector3.one * pulse;
    }
}
