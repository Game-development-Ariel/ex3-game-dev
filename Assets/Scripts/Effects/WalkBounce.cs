using UnityEngine;

/// <summary>
/// Squashes and stretches the player's body a little while walking, like tired, careful steps.
/// The character is a single picture, so this small motion is what makes it feel like it walks.
/// Put it on the Body child, and give it the player's Rigidbody2D.
/// </summary>
public class WalkBounce : MonoBehaviour
{
    // Slower than this counts as standing still.
    private const float MinWalkSpeed = 0.3f;

    [SerializeField] private Rigidbody2D body;

    [Tooltip("Bounces per second while walking")]
    [SerializeField] private float stepsPerSecond = 4f;

    [Tooltip("How strong the squash is. 0.08 = 8 percent")]
    [SerializeField] private float amount = 0.045f;

    private Vector3 baseScale;
    private float phase;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        bool isWalking = body != null && body.linearVelocity.magnitude > MinWalkSpeed;
        if (isWalking)
        {
            phase += Time.deltaTime * stepsPerSecond * Mathf.PI;
        }
        else
        {
            // Eases back to the rest shape instead of stopping in the middle of a squash.
            phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, Time.deltaTime * 10f);
        }

        // Abs(Sin) gives one bump per step. Wider and shorter on the bump, like weight landing on a foot.
        float bump = Mathf.Abs(Mathf.Sin(phase)) * amount;
        transform.localScale = new Vector3(baseScale.x * (1f + bump), baseScale.y * (1f - bump), baseScale.z);
    }
}
