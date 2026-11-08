using UnityEngine;

/// <summary>
/// The water bottle. When the player touches it, the player picks it up and the bottle disappears from the floor.
/// Needs a Collider2D with "Is Trigger" on, so the player walks into it instead of bumping into it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaterBottle : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.PickUpBottle();
        Destroy(gameObject);
    }
}
