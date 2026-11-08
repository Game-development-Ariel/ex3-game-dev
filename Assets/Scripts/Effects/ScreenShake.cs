using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Shakes the camera for a moment: ScreenShake.Shake(0.3f, 0.4f).
/// CameraFollow moves the camera every frame, so we can't just move it ourselves.
/// Instead the shake is added only while the camera draws the frame, and removed right after,
/// so CameraFollow never sees it.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ScreenShake : MonoBehaviour
{
    private static ScreenShake instance;

    private Camera cam;
    private float strength;
    private float duration;
    private float timeLeft;
    private Vector3 offset;

    /// <summary>Starts a shake. Strength is in world units, and it fades out over the seconds.</summary>
    public static void Shake(float strength, float seconds)
    {
        if (instance == null)
        {
            return;
        }
        instance.strength = strength;
        instance.duration = seconds;
        instance.timeLeft = seconds;
    }

    private void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
    }

    // The render pipeline calls these right before and right after it draws each camera.
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeforeDraw;
        RenderPipelineManager.endCameraRendering += AfterDraw;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeDraw;
        RenderPipelineManager.endCameraRendering -= AfterDraw;
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;
    }

    private void BeforeDraw(ScriptableRenderContext context, Camera drawing)
    {
        if (drawing != cam || timeLeft <= 0f)
        {
            return;
        }
        offset = Random.insideUnitCircle * strength * (timeLeft / duration);
        transform.position += offset;
    }

    private void AfterDraw(ScriptableRenderContext context, Camera drawing)
    {
        if (drawing != cam)
        {
            return;
        }
        transform.position -= offset;
        offset = Vector3.zero;
    }
}
