using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Draws the whole map in the top-right corner of the screen, with a dot marking the player.
/// A second camera renders the map into a texture, and a UI image shows that texture.
/// The camera and the UI are created here in code, when the game starts.
/// </summary>
public class Minimap : MonoBehaviour
{
    // The texture has more pixels than its size on the screen, so the map stays sharp.
    private const float TextureResolutionScale = 2f;

    // Bits of the texture's depth buffer.
    private const int TextureDepthBits = 16;

    // The camera has to stand in front of the map, which is at z = 0.
    private const float MinimapCameraZ = -10f;

    // The UI sizes below are measured on a screen whose short side has this many pixels.
    // We scale by the short side because it stays the same when the phone is rotated,
    // so the minimap keeps its size in both landscape and portrait.
    private const float ReferenceShortSide = 1080f;

    [Tooltip("The tilemap that the minimap shows")]
    [SerializeField] private Tilemap mapBounds;

    [Tooltip("The object that the dot follows")]
    [SerializeField] private Transform player;

    [Tooltip("Height of the map on the screen, in UI pixels. The width follows the shape of the map")]
    [SerializeField] private float height = 260f;

    [Tooltip("Gap from the edges of the screen, in UI pixels")]
    [SerializeField] private float margin = 20f;

    [Tooltip("Thickness of the frame around the map, in UI pixels")]
    [SerializeField] private float border = 4f;

    [Tooltip("Size of the player dot, in UI pixels")]
    [SerializeField] private float dotSize = 10f;

    [Tooltip("Color of the frame around the map")]
    [SerializeField] private Color frameColor = new Color(0f, 0f, 0f, 0.85f);

    [Tooltip("Color of the player dot")]
    [SerializeField] private Color playerColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("Shows or hides the minimap. The M key by default")]
    [SerializeField]
    private InputAction toggleAction = new InputAction(type: InputActionType.Button);

    // The area of the map, in world coordinates.
    private Bounds worldBounds;

    private Camera minimapCamera;
    private RenderTexture texture;
    private GameObject canvasObject;
    private CanvasScaler scaler;
    private RectTransform playerDot;

    // The screen size we last scaled the UI to, so we only recalculate when it changes.
    private int lastScreenWidth;
    private int lastScreenHeight;

    /// <summary>
    /// Adds the default key, so the action works without setting it in the Inspector.
    /// Unity runs this in the editor when the component is loaded or changed.
    /// </summary>
    private void OnValidate()
    {
        if (toggleAction.bindings.Count == 0)
        {
            toggleAction.AddBinding("<Keyboard>/m");
        }
    }

    // An InputAction reads nothing until it is enabled.
    private void OnEnable()
    {
        toggleAction.Enable();
    }

    private void OnDisable()
    {
        toggleAction.Disable();
    }

    /// <summary>Finds the area of the map and builds the minimap around it.</summary>
    private void Start()
    {
        mapBounds.CompressBounds(); // Shrinks the bounds to the cells that have tiles.
        worldBounds = mapBounds.localBounds;
        worldBounds.center = mapBounds.transform.TransformPoint(worldBounds.center);

        float aspect = worldBounds.size.x / worldBounds.size.y;

        int textureWidth = Mathf.RoundToInt(height * aspect * TextureResolutionScale);
        int textureHeight = Mathf.RoundToInt(height * TextureResolutionScale);
        texture = new RenderTexture(textureWidth, textureHeight, TextureDepthBits);

        CreateCamera(texture);
        CreateUI(texture, aspect);
        UpdateUIScale();
    }

    /// <summary>The texture is not freed automatically, so we release it with the minimap.</summary>
    private void OnDestroy()
    {
        if (texture != null)
        {
            texture.Release();
        }
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateUIScale();
        }

        if (toggleAction.WasPressedThisFrame())
        {
            ToggleVisible();
        }

        if (player != null)
        {
            MoveDotToPlayer();
        }
    }

    /// <summary>Scales the UI by the short side of the screen, so rotating the phone doesn't change its size.</summary>
    private void UpdateUIScale()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        int shortSide = Mathf.Min(Screen.width, Screen.height);
        scaler.scaleFactor = shortSide / ReferenceShortSide;
    }

    /// <summary>Shows or hides the minimap. The camera stops too, so it costs nothing while hidden.</summary>
    private void ToggleVisible()
    {
        bool visible = !canvasObject.activeSelf;
        canvasObject.SetActive(visible);
        minimapCamera.enabled = visible;
    }

    /// <summary>Puts the dot at the same relative place on the minimap as the player on the map.</summary>
    private void MoveDotToPlayer()
    {
        // InverseLerp gives the place between the two edges as a number from 0 to 1.
        Vector2 relativePosition = new Vector2(
            Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, player.position.x),
            Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, player.position.y));

        // Anchors are relative to the parent, from (0,0) at the bottom left to (1,1) at the top right.
        playerDot.anchorMin = relativePosition;
        playerDot.anchorMax = relativePosition;
    }

    /// <summary>Creates the camera that sees the whole map and draws it into the texture.</summary>
    private void CreateCamera(RenderTexture texture)
    {
        var cameraObject = new GameObject("Minimap Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, MinimapCameraZ);

        minimapCamera = cameraObject.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = worldBounds.extents.y; // Half of the map's height.
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = Color.black;
        minimapCamera.targetTexture = texture; // Draws into the texture instead of the screen.
    }

    /// <summary>Creates the canvas, the frame in the corner, the map image and the player dot.</summary>
    private void CreateUI(RenderTexture texture, float aspect)
    {
        canvasObject = new GameObject("Minimap Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        // We set the scale ourselves in UpdateUIScale, so the scaler doesn't change it on its own.
        scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // The frame: anchored to the top-right corner, and a bit bigger than the map.
        Image frame = CreateGraphic<Image>("Frame", canvasObject.transform);
        frame.color = frameColor;
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = Vector2.one;
        frameRect.anchorMax = Vector2.one;
        frameRect.pivot = Vector2.one;
        frameRect.anchoredPosition = new Vector2(-margin, -margin);
        frameRect.sizeDelta = new Vector2(height * aspect + border * 2f, height + border * 2f);

        // The map: fills the frame, except for the border on each side.
        RawImage map = CreateGraphic<RawImage>("Map", frame.transform);
        map.texture = texture;
        RectTransform mapRect = map.rectTransform;
        mapRect.anchorMin = Vector2.zero;
        mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = new Vector2(border, border);
        mapRect.offsetMax = new Vector2(-border, -border);

        // The dot: a child of the map, so its anchors are relative to the map.
        Image dot = CreateGraphic<Image>("Player Dot", map.transform);
        dot.color = playerColor;
        playerDot = dot.rectTransform;
        playerDot.sizeDelta = new Vector2(dotSize, dotSize);
        playerDot.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Creates a UI object of type T (like Image or RawImage) under the parent.
    /// </summary>
    private static T CreateGraphic<T>(string name, Transform parent) where T : Graphic
    {
        var graphicObject = new GameObject(name, typeof(RectTransform), typeof(T));
        graphicObject.transform.SetParent(parent, false);

        var graphic = graphicObject.GetComponent<T>();
        graphic.raycastTarget = false; // The minimap doesn't need to catch mouse clicks.
        return graphic;
    }
}
