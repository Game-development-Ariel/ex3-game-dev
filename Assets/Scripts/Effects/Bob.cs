using UnityEngine;

/// <summary>
/// Floats an object up and down and gently pulses its size, so it looks alive and catches the eye.
/// Used on the water bottle and on the arrow that shows where to go.
/// </summary>
public class Bob : MonoBehaviour
{
    [Tooltip("How high it floats, in world units")]
    [SerializeField] private float height = 0.06f;

    [Tooltip("Floats per second")]
    [SerializeField] private float speed = 0.6f;

    [Tooltip("How much the size grows and shrinks. 0.05 = 5 percent")]
    [SerializeField] private float pulse = 0.03f;

    private Vector3 startPosition;
    private Vector3 startScale;

    private void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * speed * 2f * Mathf.PI);
        transform.localPosition = startPosition + Vector3.up * wave * height;
        transform.localScale = startScale * (1f + wave * pulse);
    }
}
