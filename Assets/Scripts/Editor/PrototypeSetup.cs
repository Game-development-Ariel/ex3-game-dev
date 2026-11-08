using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools > Setup Prototype: creates everything the tutorial needs around the map, in one click.
/// - A placeholder sprite and the WaterBottle prefab (replace the sprite with the AI-made one later).
/// - The EndScreen scene, and the scene list for the build.
/// - In the open Tutorial scene: the GameManager, the bottle spawner, the mattress goal, the HUD,
///   and the player's slower tutorial speed.
/// Running it again is safe: things that already exist are kept.
/// </summary>
public static class PrototypeSetup
{
    private const string LevelScenePath = "Assets/Scenes/Tutorial.unity";
    private const string EndScenePath = "Assets/Scenes/EndScreen.unity";
    private const string BottleSpritePath = "Assets/Sprites/Items/WaterBottle.png";
    private const string BottlePrefabPath = "Assets/Prefabs/WaterBottle.prefab";

    // Drawn above the floor and decor tilemaps.
    private const int BottleSortingOrder = 5;

    // The trigger is a bit smaller than a tile, so the player has to really reach the bottle.
    private const float BottleTriggerRadius = 0.35f;

    // Extra size around the mattress tiles, so touching its edge counts.
    private const float GoalPadding = 0.4f;

    // From the design document: in the tutorial the hero is hurt, so he walks 2 tiles per second instead of 3.
    // Tested against the camera timing: this still leaves a safe window of about 2.5 seconds in every 10.
    private const float TutorialPlayerSpeed = 2f;

    [MenuItem("Tools/Setup Prototype")]
    private static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop the game first (the Play button). Changes made while playing are lost when it stops.");
            return;
        }

        Scene level = SceneManager.GetActiveScene();
        if (level.path != LevelScenePath)
        {
            Debug.LogError("Open the Tutorial scene first, then run Tools > Setup Prototype.");
            return;
        }

        GameObject bottlePrefab = CreateBottlePrefab();
        CreateEndScene();
        SetBuildScenes();
        SetupLevel(bottlePrefab);

        // Saves right away, so the result is never lost by forgetting Cmd+S.
        EditorSceneManager.MarkSceneDirty(level);
        EditorSceneManager.SaveScene(level);
        Debug.Log("Prototype setup done and saved.");
    }

    // ---------- Bottle ----------

    private static GameObject CreateBottlePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BottlePrefabPath);
        if (existing != null)
        {
            return existing;
        }

        Sprite sprite = CreatePlaceholderSprite();

        var bottle = new GameObject("WaterBottle");
        SpriteRenderer spriteRenderer = bottle.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = BottleSortingOrder;
        CircleCollider2D trigger = bottle.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = BottleTriggerRadius;
        bottle.AddComponent<WaterBottle>();

        Directory.CreateDirectory(Path.GetDirectoryName(BottlePrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bottle, BottlePrefabPath);
        Object.DestroyImmediate(bottle);
        return prefab;
    }

    /// <summary>A simple blue bottle. Only a stand-in until the AI-made sprite is ready.</summary>
    private static Sprite CreatePlaceholderSprite()
    {
        Color water = new Color(0.25f, 0.6f, 1f, 1f);
        return PlaceholderSprites.LoadOrCreate(BottleSpritePath, (x, y) =>
        {
            if (PlaceholderSprites.InRect(x, y, 13, 26, 18, 28))
            {
                return Color.white; // cap
            }
            if (PlaceholderSprites.InRect(x, y, 11, 4, 20, 22) || PlaceholderSprites.InRect(x, y, 13, 23, 18, 25))
            {
                return water; // body and neck
            }
            return Color.clear;
        });
    }

    // ---------- End screen ----------

    private static void CreateEndScene()
    {
        if (File.Exists(EndScenePath))
        {
            return;
        }

        Scene endScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        SceneManager.MoveGameObjectToScene(cameraObject, endScene);

        var screen = new GameObject("EndScreen");
        screen.AddComponent<EndScreen>();
        SceneManager.MoveGameObjectToScene(screen, endScene);

        EditorSceneManager.SaveScene(endScene, EndScenePath);
        EditorSceneManager.CloseScene(endScene, true);
    }

    /// <summary>The scenes that go into the build, in order. The first one is where the game starts.</summary>
    private static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(LevelScenePath, true),
            new EditorBuildSettingsScene(EndScenePath, true),
        };

        // Writes the project settings (with the scene list) to disk now, and not only when Unity quits.
        AssetDatabase.SaveAssets();
    }

    // ---------- Level ----------

    private static void SetupLevel(GameObject bottlePrefab)
    {
        if (Object.FindAnyObjectByType<GameManager>() == null)
        {
            var manager = new GameObject("GameManager");
            manager.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(manager, "Setup Prototype");
        }

        if (Object.FindAnyObjectByType<StatusHud>() == null)
        {
            var hud = new GameObject("StatusHud");
            hud.AddComponent<StatusHud>();
            Undo.RegisterCreatedObjectUndo(hud, "Setup Prototype");
        }

        GameObject spawnPoint = GameObject.Find("BottleSpawn");
        if (spawnPoint == null)
        {
            Debug.LogError("No BottleSpawn in the scene. Run Tools > Build Tutorial Map first.");
        }
        else
        {
            BottleSpawner spawner = spawnPoint.GetComponent<BottleSpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<BottleSpawner>(spawnPoint);
            }
            SerializedObject fields = new SerializedObject(spawner);
            fields.FindProperty("bottlePrefab").objectReferenceValue = bottlePrefab;
            fields.ApplyModifiedProperties();
        }

        CreateMattressGoal();
        SetPlayerSpeed();
    }

    private static void SetPlayerSpeed()
    {
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("No player in the scene.");
            return;
        }
        SerializedObject fields = new SerializedObject(player);
        fields.FindProperty("moveSpeed").floatValue = TutorialPlayerSpeed;
        fields.ApplyModifiedProperties();
    }

    /// <summary>Puts a trigger box over the mattress tiles of the Decor tilemap.</summary>
    private static void CreateMattressGoal()
    {
        GameObject decorObject = GameObject.Find("TunnelGrid/Decor");
        Tilemap decor = decorObject != null ? decorObject.GetComponent<Tilemap>() : null;
        if (decor == null)
        {
            Debug.LogError("No TunnelGrid/Decor tilemap in the scene.");
            return;
        }

        bool found = false;
        Bounds mattress = new Bounds();
        foreach (Vector3Int cell in decor.cellBounds.allPositionsWithin)
        {
            TileBase tile = decor.GetTile(cell);
            if (tile == null || !tile.name.StartsWith("Mattress"))
            {
                continue;
            }
            Bounds tileBounds = new Bounds(decor.GetCellCenterWorld(cell), decor.cellSize);
            if (found)
            {
                mattress.Encapsulate(tileBounds);
            }
            else
            {
                mattress = tileBounds;
                found = true;
            }
        }
        if (!found)
        {
            Debug.LogError("No mattress tiles on the Decor tilemap.");
            return;
        }

        GameObject goal = GameObject.Find("MattressGoal");
        if (goal == null)
        {
            goal = new GameObject("MattressGoal");
            Undo.RegisterCreatedObjectUndo(goal, "Setup Prototype");
            goal.AddComponent<BoxCollider2D>().isTrigger = true;
            goal.AddComponent<MattressGoal>();
        }
        goal.transform.position = mattress.center;
        BoxCollider2D box = goal.GetComponent<BoxCollider2D>();
        box.size = (Vector2)mattress.size + Vector2.one * GoalPadding;
    }
}
