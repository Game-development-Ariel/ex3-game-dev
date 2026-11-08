using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Paints the tutorial level from the text layout below, using the same tile rules as the ex2 tunnel:
/// a wall with floor below it shows its front face, floor next to a wall gets a shadow,
/// and a wall next to floor gets a rim. Run it from Tools > Build Tutorial Map.
/// To change the map, edit LAYOUT and run the menu again.
/// </summary>
public static class TutorialMapBuilder
{
    // Every character is one tile. Row 0 is the top (north) of the map.
    //  #  wall                     .  floor
    //  m  wall with tally marks    p  wall with a pipe     (only on a wall that has floor below it)
    //  c  wall with the security camera. It looks at the floor next to it
    //  S  mattress, left half      s  mattress, right half
    //  N  journal (note)           @  player start
    //  K  sharp concrete (rubble)  k  small rubble
    //  D  door                     P  puddle
    //  B  barrel                   C  crate
    //  T  table                    W  where the water bottle goes (marked by the BottleSpawn object)
    private static readonly string[] LAYOUT =
    {
        "######mm#################################",
        "#.........###############################",
        "#.Ss......###ppp############CC....T..####",
        "#.N@......#.....CC.....#####C......W.####",
        "#.........D............#####.........####",
        "#.........#.k......B...#####.........####",
        "#......P..##########...#####.k.......####",
        "#K........##########..B#####........B####",
        "####################.P.########..########",
        "####################...###pp###..########",
        "####################.........B.......C..c",
        "####################........P..........D#",
        "####################.....CC.......BB....#",
        "#########################################",
    };

    // How many wall tiles to add around the layout, so the camera never shows empty space.
    private const int MARGIN = 4;

    // The layout column and row that sit at world cell (0, 0). Here: the corridor's first floor tile, middle row.
    private const int ORIGIN_COLUMN = 11;
    private const int ORIGIN_ROW = 4;

    // Fixed seed, so the random floor and wall variations come out the same on every run.
    private const int SEED = 7;

    private const string TILES_FOLDER = "Assets/Tiles/Tunnel/";

    // The camera's picture. Drawn with the lens pointing up, because "up" is where the camera looks.
    private const string CAMERA_SPRITE_PATH = "Assets/Sprites/Items/SecurityCamera.png";

    // Above all the tilemaps (the Decor layer is 4), so the camera is drawn on top of the wall.
    private const int CAMERA_SORTING_ORDER = 6;

    // The vision cone is drawn on the floor but under the walls (the Walls layer is 2),
    // so a wall always covers it. Same order as FloorShadow, and a bit closer to the camera so it draws above it.
    private const int CONE_SORTING_ORDER = 1;
    private const float CONE_Z = -0.1f;

    // An unlit material: the cone keeps its own color in the dark tunnel, like a beam of light.
    private const string CONE_MATERIAL_PATH = "Assets/Materials/VisionCone.mat";
    private const string CONE_SHADER = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

    [MenuItem("Tools/Build Tutorial Map")]
    private static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop the game first (the Play button). Changes made while playing are lost when it stops.");
            return;
        }

        GameObject grid = GameObject.Find("TunnelGrid");
        if (grid == null)
        {
            Debug.LogError("No TunnelGrid in the open scene. Open the Tutorial scene first.");
            return;
        }

        Tilemap floor = FindLayer(grid, "Floor");
        Tilemap shadow = FindLayer(grid, "FloorShadow");
        Tilemap walls = FindLayer(grid, "Walls");
        Tilemap details = FindLayer(grid, "WallDetails");
        Tilemap decor = FindLayer(grid, "Decor");
        if (floor == null || shadow == null || walls == null || details == null || decor == null)
        {
            return;
        }

        for (int row = 1; row < LAYOUT.Length; row++)
        {
            if (LAYOUT[row].Length != LAYOUT[0].Length)
            {
                Debug.LogError($"Row {row} of LAYOUT has {LAYOUT[row].Length} characters instead of {LAYOUT[0].Length}.");
                return;
            }
        }

        // The ex2 tunnel lights don't belong in the tutorial.
        foreach (string leftover in new[] { "Lights", "ExitLightPool" })
        {
            GameObject obj = GameObject.Find(leftover);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        Tilemap[] layers = { floor, shadow, walls, details, decor };
        Undo.RegisterCompleteObjectUndo(layers, "Build Tutorial Map");
        foreach (Tilemap layer in layers)
        {
            layer.ClearAllTiles();
        }

        TileBase[] floorTiles = LoadTiles("Floor0", "Floor1", "Floor2", "Floor3");
        TileBase[] wallTops = LoadTiles("WallTop0", "WallTop1");
        TileBase[] wallFaces = LoadTiles("WallFace0", "WallFace1", "WallFace2");
        System.Random random = new System.Random(SEED);

        int width = LAYOUT[0].Length;
        int height = LAYOUT.Length;
        Vector3Int playerCell = Vector3Int.zero;
        Vector3Int bottleCell = Vector3Int.zero;
        Vector3Int cameraCell = Vector3Int.zero;

        for (int row = -MARGIN; row < height + MARGIN; row++)
        {
            for (int column = -MARGIN; column < width + MARGIN; column++)
            {
                char symbol = SymbolAt(column, row);
                Vector3Int cell = ToCell(column, row);

                if (IsFloor(symbol))
                {
                    floor.SetTile(cell, floorTiles[random.Next(floorTiles.Length)]);
                    shadow.SetTile(cell, FloorShadow(column, row));
                    decor.SetTile(cell, DecorTile(symbol));
                    if (symbol == '@')
                    {
                        playerCell = cell;
                    }
                    else if (symbol == 'W')
                    {
                        bottleCell = cell;
                    }
                    continue;
                }

                if (symbol == 'c')
                {
                    cameraCell = cell;
                }

                // A wall. If there is floor right below it, the player sees its front face.
                bool floorBelow = IsFloor(SymbolAt(column, row + 1));
                if (floorBelow)
                {
                    walls.SetTile(cell, wallFaces[random.Next(wallFaces.Length)]);
                    details.SetTile(cell, FaceDetail(symbol));
                }
                else
                {
                    walls.SetTile(cell, wallTops[random.Next(wallTops.Length)]);
                    details.SetTile(cell, WallRim(column, row));
                }
            }
        }

        walls.CompressBounds();
        floor.CompressBounds();
        Transform playerStart = PlaceMarker("PlayerStart", floor.GetCellCenterWorld(playerCell));
        GameObject player = MovePlayer(floor, playerCell);
        PlaceMarker("BottleSpawn", floor.GetCellCenterWorld(bottleCell));
        PlaceSecurityCamera(floor, cameraCell, CameraFacing(CameraColumn(), CameraRow()), player, playerStart);
        // Saves right away, so the result is never lost by forgetting Cmd+S.
        EditorSceneManager.MarkSceneDirty(grid.scene);
        EditorSceneManager.SaveScene(grid.scene);
        Debug.Log("Tutorial map built and saved.");
    }

    private static Tilemap FindLayer(GameObject grid, string name)
    {
        Transform child = grid.transform.Find(name);
        Tilemap tilemap = child != null ? child.GetComponent<Tilemap>() : null;
        if (tilemap == null)
        {
            Debug.LogError($"TunnelGrid has no tilemap layer called {name}.");
        }
        return tilemap;
    }

    private static TileBase[] LoadTiles(params string[] names)
    {
        TileBase[] tiles = new TileBase[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            tiles[i] = LoadTile(names[i]);
        }
        return tiles;
    }

    private static TileBase LoadTile(string name)
    {
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(TILES_FOLDER + name + ".asset");
        if (tile == null)
        {
            Debug.LogError($"Missing tile {TILES_FOLDER}{name}.asset");
        }
        return tile;
    }

    // Anything outside the layout counts as wall.
    private static char SymbolAt(int column, int row)
    {
        if (row < 0 || row >= LAYOUT.Length || column < 0 || column >= LAYOUT[row].Length)
        {
            return '#';
        }
        return LAYOUT[row][column];
    }

    private static bool IsFloor(char symbol)
    {
        return symbol != '#' && symbol != 'm' && symbol != 'p' && symbol != 'c';
    }

    // Rows go down in the layout but up in the world, so the row is flipped.
    private static Vector3Int ToCell(int column, int row)
    {
        return new Vector3Int(column - ORIGIN_COLUMN, ORIGIN_ROW - row, 0);
    }

    // Same order as the ex2 map: a wall to the north wins, then east, then west, then south.
    private static TileBase FloorShadow(int column, int row)
    {
        if (!IsFloor(SymbolAt(column, row - 1)))
        {
            return LoadTile("ShadowN");
        }
        if (!IsFloor(SymbolAt(column + 1, row)))
        {
            return LoadTile("ShadowE");
        }
        if (!IsFloor(SymbolAt(column - 1, row)))
        {
            return LoadTile("ShadowW");
        }
        if (!IsFloor(SymbolAt(column, row + 1)))
        {
            return LoadTile("ShadowS");
        }
        return null;
    }

    // The rim is a thin edge drawn on the side of the wall that touches floor.
    private static TileBase WallRim(int column, int row)
    {
        if (IsFloor(SymbolAt(column, row - 1)))
        {
            return LoadTile("RimN");
        }
        if (IsFloor(SymbolAt(column + 1, row)))
        {
            return LoadTile("RimE");
        }
        if (IsFloor(SymbolAt(column - 1, row)))
        {
            return LoadTile("RimW");
        }
        return null;
    }

    private static TileBase FaceDetail(char symbol)
    {
        switch (symbol)
        {
            case 'm': return LoadTile("WallMarks");
            case 'p': return LoadTile("Pipe");
            default: return null;
        }
    }

    private static TileBase DecorTile(char symbol)
    {
        switch (symbol)
        {
            case 'S': return LoadTile("MattressL");
            case 's': return LoadTile("MattressR");
            case 'N': return LoadTile("Note");
            case 'K': return LoadTile("RubbleA");
            case 'k': return LoadTile("RubbleB");
            case 'D': return LoadTile("Door");
            case 'P': return LoadTile("Puddle");
            case 'B': return LoadTile("Barrel");
            case 'C': return LoadTile("Crate");
            case 'T': return LoadTile("Table");
            default: return null;
        }
    }

    // An empty object that marks a place: where the player starts, where the bottle will appear.
    private static Transform PlaceMarker(string name, Vector3 position)
    {
        GameObject marker = GameObject.Find(name);
        if (marker == null)
        {
            marker = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(marker, "Build Tutorial Map");
        }
        Undo.RecordObject(marker.transform, "Build Tutorial Map");
        marker.transform.position = position;
        return marker.transform;
    }

    private static int CameraRow()
    {
        for (int row = 0; row < LAYOUT.Length; row++)
        {
            if (LAYOUT[row].IndexOf('c') >= 0)
            {
                return row;
            }
        }
        return -1;
    }

    private static int CameraColumn()
    {
        int row = CameraRow();
        return row >= 0 ? LAYOUT[row].IndexOf('c') : -1;
    }

    // The direction from the camera's wall to the floor next to it.
    private static Vector2Int CameraFacing(int column, int row)
    {
        if (IsFloor(SymbolAt(column, row + 1)))
        {
            return Vector2Int.down;
        }
        if (IsFloor(SymbolAt(column - 1, row)))
        {
            return Vector2Int.left;
        }
        if (IsFloor(SymbolAt(column + 1, row)))
        {
            return Vector2Int.right;
        }
        return Vector2Int.up;
    }

    // The camera hangs on the edge of its wall tile, so its view starts at the floor and not inside the wall.
    // It is rebuilt on every run, so it always gets the default values from SecurityCamera.cs.
    private static void PlaceSecurityCamera(Tilemap floor, Vector3Int cell, Vector2Int facing, GameObject player, Transform respawnPoint)
    {
        GameObject old = GameObject.Find("SecurityCamera");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }
        if (CameraRow() < 0)
        {
            return;
        }

        GameObject cameraObject = new GameObject("SecurityCamera");
        Undo.RegisterCreatedObjectUndo(cameraObject, "Build Tutorial Map");
        SecurityCamera securityCamera = cameraObject.AddComponent<SecurityCamera>();

        // The cone is a child, so it turns with the camera. SecurityCamera rebuilds its mesh every frame.
        var cone = new GameObject("VisionCone");
        cone.transform.SetParent(cameraObject.transform, false);
        cone.transform.localPosition = new Vector3(0f, 0f, CONE_Z);
        MeshFilter coneFilter = cone.AddComponent<MeshFilter>();
        MeshRenderer coneRenderer = cone.AddComponent<MeshRenderer>();
        coneRenderer.sharedMaterial = ConeMaterial();
        coneRenderer.sortingOrder = CONE_SORTING_ORDER;

        // The picture is a child too, so it turns together with the camera and its cone.
        var body = new GameObject("Body");
        body.transform.SetParent(cameraObject.transform, false);
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = CameraSprite();
        bodyRenderer.sortingOrder = CAMERA_SORTING_ORDER;

        Vector3 center = floor.GetCellCenterWorld(cell);
        cameraObject.transform.position = center + new Vector3(facing.x, facing.y, 0f) * 0.6f;

        // Unity angles: 0 looks up, 90 left, 180 down, 270 right.
        float angle = Vector2.SignedAngle(Vector2.up, facing);
        if (angle < 0f)
        {
            angle += 360f;
        }
        cameraObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Fills the camera's fields, like dragging them in the Inspector.
        SerializedObject fields = new SerializedObject(securityCamera);
        fields.FindProperty("centerAngle").floatValue = angle;
        fields.FindProperty("visionCone").objectReferenceValue = coneFilter;
        fields.FindProperty("player").objectReferenceValue = player != null ? player.transform : null;
        fields.FindProperty("respawnPoint").objectReferenceValue = respawnPoint;
        fields.ApplyModifiedProperties();
    }

    /// <summary>
    /// The material is saved as an asset, so it goes into the build. A material made only in code at runtime
    /// could lose its shader in the build, because Unity leaves out shaders that nothing references.
    /// </summary>
    private static Material ConeMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CONE_MATERIAL_PATH);
        if (material == null)
        {
            material = new Material(Shader.Find(CONE_SHADER));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CONE_MATERIAL_PATH));
            AssetDatabase.CreateAsset(material, CONE_MATERIAL_PATH);
        }
        return material;
    }

    /// <summary>A gray box with a dark lens on top. Only a stand-in until the AI-made sprite is ready.</summary>
    private static Sprite CameraSprite()
    {
        Color casing = new Color(0.6f, 0.6f, 0.62f, 1f);
        Color lens = new Color(0.1f, 0.1f, 0.12f, 1f);
        Color glint = new Color(0.4f, 0.7f, 1f, 1f);
        Color mount = new Color(0.3f, 0.3f, 0.32f, 1f);
        return PlaceholderSprites.LoadOrCreate(CAMERA_SPRITE_PATH, (x, y) =>
        {
            if (PlaceholderSprites.InRect(x, y, 15, 24, 16, 25))
            {
                return glint;
            }
            if (PlaceholderSprites.InRect(x, y, 12, 21, 19, 27))
            {
                return lens;
            }
            if (PlaceholderSprites.InRect(x, y, 10, 8, 21, 20))
            {
                return casing;
            }
            if (PlaceholderSprites.InRect(x, y, 14, 3, 17, 7))
            {
                return mount;
            }
            return Color.clear;
        });
    }

    private static GameObject MovePlayer(Tilemap floor, Vector3Int cell)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("No Player in the scene, so the start position was not set.");
            return null;
        }
        Undo.RecordObject(player.transform, "Build Tutorial Map");
        Vector3 center = floor.GetCellCenterWorld(cell);
        player.transform.position = new Vector3(center.x, center.y, player.transform.position.z);
        return player;
    }
}
