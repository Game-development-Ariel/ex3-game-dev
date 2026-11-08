using UnityEngine;

/// <summary>
/// The area of the mattress in the cell. Bringing the water bottle here wins the level.
/// Needs a Collider2D with "Is Trigger" on, covering the mattress.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MattressGoal : MonoBehaviour
{
    // Stay and not Enter: the player starts next to the mattress, so the trigger may already be touched
    // when the player gets the bottle. Stay keeps checking every physics step.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.DeliverBottle();
    }
}
