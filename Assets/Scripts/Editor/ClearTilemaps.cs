using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Adds a menu item that removes every tile from the selected tilemaps.
/// Unity 6 no longer has "Clear All Tiles" in the Tilemap menu, so this replaces it.
/// </summary>
public static class ClearTilemaps
{
    [MenuItem("Tools/Clear Selected Tilemaps")]
    private static void ClearSelected()
    {
        int cleared = 0;
        foreach (GameObject obj in Selection.gameObjects)
        {
            Tilemap tilemap = obj.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                continue;
            }

            // Lets Cmd+Z bring the tiles back.
            Undo.RegisterCompleteObjectUndo(tilemap, "Clear Selected Tilemaps");
            tilemap.ClearAllTiles();
            cleared++;
        }

        Debug.Log($"Cleared {cleared} tilemap(s).");
    }

    // The menu item is grayed out unless at least one tilemap is selected.
    [MenuItem("Tools/Clear Selected Tilemaps", true)]
    private static bool HasTilemapSelected()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj.GetComponent<Tilemap>() != null)
            {
                return true;
            }
        }
        return false;
    }
}
