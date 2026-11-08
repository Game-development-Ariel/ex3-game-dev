using UnityEngine;

/// <summary>
/// Creates the water bottle from its prefab, at this object's position.
/// If the player is caught while carrying the bottle, the bottle is lost, so a new one appears here.
/// </summary>
public class BottleSpawner : MonoBehaviour
{
    [Tooltip("The water bottle prefab")]
    [SerializeField] private GameObject bottlePrefab;

    // The bottle that is on the floor now. Null after the player picks it up.
    private GameObject currentBottle;

    private void Update()
    {
        bool playerHasBottle = GameManager.Instance != null && GameManager.Instance.HasBottle;
        if (currentBottle == null && !playerHasBottle && bottlePrefab != null)
        {
            currentBottle = Instantiate(bottlePrefab, transform.position, Quaternion.identity);
        }
    }
}
