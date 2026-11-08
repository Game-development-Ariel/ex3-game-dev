using UnityEngine;

/// <summary>
/// A trigger that teaches: the first time the player walks into it, it shows its tip.
/// The tutorial puts these where the player needs to learn something, like before the camera.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HintZone : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string hint;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        HintToast hints = FindAnyObjectByType<HintToast>();
        if (hints != null)
        {
            hints.Show(hint);
        }
        Destroy(gameObject); // Shown once. Walking here again doesn't repeat it.
    }
}
