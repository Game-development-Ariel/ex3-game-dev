using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Setup Game Feel: adds the mood, the sound and the teaching of the tutorial to the open Tutorial scene.
/// - Light: a dim tunnel, weak flickering bulbs, light under the cell door, dust floating in the light.
/// - Post-processing: soft glow, film grain, dark edges and faded colors, which MoodVolume changes with the hero's feelings.
/// - The player: no flashlight yet (from the design, it is found later), soft footsteps, a shadow.
/// - Sound: the tunnel, the music, the heartbeat and the breathing.
/// - Teaching: the opening, the hero's thoughts, the hint zones, and two memories in the cell.
/// Run it after Tools > Build Tutorial Map. Running it again is safe: it rebuilds its own objects.
/// </summary>
public static class GameFeelSetup
{
    private const string LevelScenePath = "Assets/Scenes/Tutorial.unity";
    private const string BottlePrefabPath = "Assets/Prefabs/WaterBottle.prefab";
    private const string UiFolder = "Assets/Resources/UI/";
    private const string DustMaterialPath = "Assets/Materials/Dust.mat";
    private const string MoodProfilePath = "Assets/Settings/TunnelMood.asset";
    private const string LitSpriteShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";

    // Tutorial rules: the hero wakes up weak (60 of 100 hope), and can't drop below 25.
    private const int TutorialStartHope = 60;
    private const int TutorialMinHope = 25;

    private static readonly Color BulbColor = new Color(1f, 0.74f, 0.48f);

    [MenuItem("Tools/Setup Game Feel")]
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
            Debug.LogError("Open the Tutorial scene first, then run Tools > Setup Game Feel.");
            return;
        }

        ImportUiSprites();
        SetupBottlePrefab();
        SetupPlayer();
        SetupRules();
        SetupLights();
        SetupPostProcessing();
        SetupSound();
        SetupTeaching();

        // Saves right away, so the result is never lost by forgetting Cmd+S.
        EditorSceneManager.MarkSceneDirty(level);
        EditorSceneManager.SaveScene(level);
        Debug.Log("Game feel setup done and saved.");
    }

    // ---------- Sprites ----------

    /// <summary>
    /// The UI sprites are smooth (Bilinear). The frames get a border, so a sliced Image stretches
    /// only their middle and keeps the rounded corners.
    /// </summary>
    private static void ImportUiSprites()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiFolder.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            float border = 0f;
            if (name == "Panel" || name == "Button")
            {
                border = 16f;
            }
            else if (name == "Key")
            {
                border = 12f;
            }
            else if (name.StartsWith("Bar"))
            {
                border = 7f;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = Vector4.one * border;
            importer.SaveAndReimport();
        }
    }

    // ---------- Objects ----------

    /// <summary>The bottle floats a little, has a faint cold glow, and a soft shadow on the floor.</summary>
    private static void SetupBottlePrefab()
    {
        GameObject bottle = PrefabUtility.LoadPrefabContents(BottlePrefabPath);
        Object.DestroyImmediate(bottle.GetComponent<Bob>());
        bottle.AddComponent<Bob>();

        SpriteRenderer glow = ChildSprite(bottle.transform, "Glow", "Glow", new Color(0.6f, 0.78f, 1f, 0.3f), 3);
        glow.transform.localScale = Vector3.one * 1.6f;
        SpriteRenderer shadow = ChildSprite(bottle.transform, "Shadow", "Shadow", new Color(1f, 1f, 1f, 0.55f), 4);
        shadow.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        shadow.transform.localScale = new Vector3(0.9f, 0.7f, 1f);

        PrefabUtility.SaveAsPrefabAsset(bottle, BottlePrefabPath);
        PrefabUtility.UnloadPrefabContents(bottle);
    }

    /// <summary>
    /// The hero has no flashlight yet, only eyes that got used to the dark: a soft, small, warm halo.
    /// Soft footsteps, a gentle walking bounce, and a shadow that grounds the character on the floor.
    /// </summary>
    private static void SetupPlayer()
    {
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        Transform body = player.transform.Find("Body");

        Transform flashlight = body.Find("Flashlight");
        if (flashlight != null)
        {
            flashlight.gameObject.SetActive(false);
        }
        Light2D halo = body.GetComponent<Light2D>();
        if (halo != null)
        {
            halo.pointLightOuterRadius = 3.4f;
            halo.pointLightInnerRadius = 0.4f;
            halo.intensity = 0.75f;
            halo.color = new Color(1f, 0.88f, 0.74f);
        }

        Object.DestroyImmediate(player.GetComponent<Footsteps>());
        Footsteps footsteps = player.gameObject.AddComponent<Footsteps>(); // Also adds the AudioSource it needs.
        footsteps.GetComponent<AudioSource>().playOnAwake = false;

        Object.DestroyImmediate(body.GetComponent<WalkBounce>());
        WalkBounce bounce = body.gameObject.AddComponent<WalkBounce>();
        SetField(bounce, "body", player.GetComponent<Rigidbody2D>());

        SpriteRenderer shadow = ChildSprite(player.transform, "Shadow", "Shadow", new Color(1f, 1f, 1f, 0.6f), 9);
        shadow.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        shadow.transform.localScale = new Vector3(1.2f, 0.9f, 1f);
    }

    private static void SetupRules()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        SerializedObject fields = new SerializedObject(manager);
        fields.FindProperty("startHealth").intValue = TutorialStartHope;
        fields.FindProperty("minHealth").intValue = TutorialMinHope;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Light ----------

    /// <summary>
    /// The tunnel is almost dark. A few weak bulbs show the way, each flickering on its own.
    /// The storage room is a little warmer and brighter: the place of water, the place of hope.
    /// Dust floats in the light of the rooms. Positions are world coordinates of the tutorial map.
    /// </summary>
    private static void SetupLights()
    {
        GameObject globalLight = GameObject.Find("Global Light 2D");
        if (globalLight != null)
        {
            Light2D ambient = globalLight.GetComponent<Light2D>();
            ambient.intensity = 0.24f;
            ambient.color = new Color(0.55f, 0.6f, 0.78f);
        }

        GameObject bulbs = Recreate("Bulbs");
        AddBulb(bulbs.transform, "Cell", new Vector2(-5.5f, 2f), 4.5f, 0.55f, BulbColor);
        AddBulb(bulbs.transform, "Under the Door", new Vector2(-0.5f, 0.5f), 1.6f, 0.5f, BulbColor);
        AddBulb(bulbs.transform, "Corridor", new Vector2(6f, 0.5f), 4.5f, 0.7f, BulbColor);
        AddBulb(bulbs.transform, "Way Down", new Vector2(10.5f, -3f), 4f, 0.6f, BulbColor);
        AddBulb(bulbs.transform, "Waiting Spot", new Vector2(15.5f, -6.5f), 3.2f, 0.4f, BulbColor);
        AddBulb(bulbs.transform, "Storage Room", new Vector2(22f, 0f), 6f, 0.95f, new Color(1f, 0.8f, 0.56f));

        Material dust = DustMaterial();
        GameObject dustParent = Recreate("Dust");
        AddDust(dustParent.transform, new Vector2(-6f, 0.5f), new Vector2(8f, 6f), dust);
        AddDust(dustParent.transform, new Vector2(6f, 0.5f), new Vector2(10f, 2.5f), dust);
        AddDust(dustParent.transform, new Vector2(22f, 0.5f), new Vector2(8f, 6f), dust);
    }

    private static void AddBulb(Transform parent, string name, Vector2 position, float radius, float intensity, Color color)
    {
        var bulb = new GameObject(name);
        bulb.transform.SetParent(parent, false);
        bulb.transform.position = position;
        Light2D light = bulb.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.15f;
        light.intensity = intensity;
        light.color = color;
        bulb.AddComponent<LightFlicker>();
    }

    private static void AddDust(Transform parent, Vector2 position, Vector2 area, Material material)
    {
        var dust = new GameObject("Dust");
        dust.transform.SetParent(parent, false);
        dust.transform.position = position;
        DustMotes motes = dust.AddComponent<DustMotes>(); // Also adds the ParticleSystem it needs.
        SetField(motes, "material", material);
        SerializedObject fields = new SerializedObject(motes);
        fields.FindProperty("area").vector2Value = area;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// A material that the 2D lights light up, with the soft glow picture.
    /// It is saved as an asset, so the build includes its shader.
    /// </summary>
    private static Material DustMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find(LitSpriteShader));
            System.IO.Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(material, DustMaterialPath);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(UiFolder + "Glow.png");
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- Post-processing ----------

    /// <summary>
    /// A global Volume with the tunnel's look. The camera must have post-processing turned on to show it.
    /// MoodVolume changes the values while the game runs.
    /// </summary>
    private static void SetupPostProcessing()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MoodProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            System.IO.Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.CreateAsset(profile, MoodProfilePath);

            Bloom bloom = AddEffect<Bloom>(profile);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.7f);

            Vignette vignette = AddEffect<Vignette>(profile);
            vignette.intensity.Override(0.36f);
            vignette.smoothness.Override(0.5f);

            FilmGrain grain = AddEffect<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.35f);

            ColorAdjustments colors = AddEffect<ColorAdjustments>(profile);
            colors.contrast.Override(14f);
            colors.saturation.Override(-22f);
            colors.postExposure.Override(0f);
            colors.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

            ChromaticAberration split = AddEffect<ChromaticAberration>(profile);
            split.intensity.Override(0f);
            AssetDatabase.SaveAssets();
        }

        GameObject volumeObject = Recreate("Mood");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        volumeObject.AddComponent<MoodVolume>();

        Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    /// <summary>Each effect is saved inside the profile asset, so it isn't lost when Unity closes.</summary>
    private static T AddEffect<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T effect = profile.Add<T>(true);
        effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(effect, profile);
        return effect;
    }

    // ---------- Sound ----------

    private static void SetupSound()
    {
        Object.DestroyImmediate(GameObject.Find("Ambience"));
        GameObject sound = Recreate("Sound");

        AudioSource ambience = LoopingSource(sound.transform, "Ambience", "Ambience", 0.35f);
        ambience.playOnAwake = true;

        AudioSource music = LoopingSource(sound.transform, "Music", "Music", 0f);
        music.playOnAwake = false;
        music.gameObject.AddComponent<MusicPlayer>();

        // The heartbeat and the breathing play all the time. Tension sets how loud they are.
        AudioSource heartbeat = LoopingSource(sound.transform, "Heartbeat", "Heartbeat", 0f);
        AudioSource breathing = LoopingSource(sound.transform, "Breathing", "Breathing", 0.2f);
        Tension tension = sound.AddComponent<Tension>();
        SetField(tension, "heartbeat", heartbeat);
        SetField(tension, "breathing", breathing);
    }

    private static AudioSource LoopingSource(Transform parent, string name, string clip, float volume)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>("Audio/" + clip);
        source.loop = true;
        source.playOnAwake = true;
        source.volume = volume;
        return source;
    }

    // ---------- Teaching ----------

    /// <summary>
    /// The opening, the hints and the objective arrow, then the hint zones and the memories on the map.
    /// The positions are world coordinates of the tutorial map (see the layout in TutorialMapBuilder).
    /// </summary>
    private static void SetupTeaching()
    {
        GameObject ui = Recreate("Tutorial UI");
        ui.AddComponent<HintToast>();
        ui.AddComponent<TitleCard>();
        ui.AddComponent<MobileJoystick>();
        ObjectiveMarker marker = ui.AddComponent<ObjectiveMarker>();
        GameObject goal = GameObject.Find("MattressGoal");
        SetField(marker, "mattress", goal != null ? goal.transform : null);

        GameObject zones = Recreate("Teaching Zones");
        foreach (string old in new[] { "Hint Door", "Hint Camera", "Hint Wait" })
        {
            Object.DestroyImmediate(GameObject.Find(old)); // Left from an older version of this tool.
        }

        AddZone<MemoryZone>(zones.transform, "Memory: Note", new Vector2(-8.5f, 1.5f), new Vector2(0.8f, 0.8f), "memory",
            "A scrap of paper under the mattress.\nI wrote my family's names on it, so I won't forget their voices.", 15);
        AddZone<MemoryZone>(zones.transform, "Memory: Wall", new Vector2(-4f, 3.5f), new Vector2(2f, 1f), "memory",
            "Lines scratched into the wall. Someone counted days here before me.\nI'll count too. And I'll go home.", 10);
        AddZone<HintZone>(zones.transform, "Hint: Door", new Vector2(0.5f, 1f), new Vector2(1f, 4f), "hint",
            "The door isn't locked all the way.\nThe corridor is dark. I follow the faint light.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Camera", new Vector2(10.5f, -3.5f), new Vector2(3f, 1f), "hint",
            "A camera. Its pale light is its eye.\nIf the light touches me, they'll know.", 0);
        AddZone<HintZone>(zones.transform, "Hint: Wait", new Vector2(18.5f, -6.5f), new Vector2(1f, 3f), "hint",
            "I wait here, in the dark, and watch the light.\nWhen it turns away, I go up. Quietly.", 0);
    }

    private static void AddZone<T>(Transform parent, string name, Vector2 position, Vector2 size, string textField, string text, int hope) where T : Component
    {
        var zone = new GameObject(name);
        zone.transform.SetParent(parent, false);
        zone.transform.position = position;
        BoxCollider2D box = zone.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;

        SerializedObject fields = new SerializedObject(zone.AddComponent<T>());
        fields.FindProperty(textField).stringValue = text;
        if (hope > 0)
        {
            fields.FindProperty("hope").intValue = hope;
        }
        fields.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Helpers ----------

    /// <summary>Deletes the object if it exists and makes a new empty one, so running the tool twice doesn't double things.</summary>
    private static GameObject Recreate(string name)
    {
        Object.DestroyImmediate(GameObject.Find(name));
        return new GameObject(name);
    }

    /// <summary>Finds or makes a child with a sprite from the UI folder.</summary>
    private static SpriteRenderer ChildSprite(Transform parent, string name, string sprite, Color color, int sortingOrder)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }
        SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + sprite + ".png");
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        return spriteRenderer;
    }

    /// <summary>Sets a private [SerializeField] field, the same way the Inspector does.</summary>
    private static void SetField(Object target, string field, Object value)
    {
        SerializedObject fields = new SerializedObject(target);
        fields.FindProperty(field).objectReferenceValue = value;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }
}
