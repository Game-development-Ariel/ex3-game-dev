using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The level's HUD, created in code when the level starts. It is kept small and quiet:
/// - top left: a small flame and a thin bar for hope, and a drop that fills when the hero carries water;
/// - under it: what the hero is thinking, which is also the goal ("I need water.");
/// - when caught: the screen goes dark for a moment, and the hero wakes up back in the cell.
/// It listens to the GameManager's events, so it reacts the moment something happens.
/// </summary>
public class StatusHud : MonoBehaviour
{
    // Higher than the minimap's canvas, so the HUD is drawn on top. The vignette is under everything.
    private const int CanvasSortingOrder = 10;
    private const int VignetteSortingOrder = -10;
    private const int BlackoutSortingOrder = 40;

    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private const string FindWater = "I need water.";
    private const string GoBack = "Back to the mattress. Quietly.";

    [Tooltip("How fast the hope bar slides to the new value, in fractions of the bar per second")]
    [SerializeField] private float barSpeed = 0.6f;

    [Tooltip("Seconds of darkness after being caught")]
    [SerializeField] private float blackoutSeconds = 2.2f;

    private RectTransform flame;
    private Image hopeFill;
    private Image waterIcon;
    private Text thought;
    private CanvasGroup blackout;
    private Text blackoutText;
    private HintToast hints;
    private PlayerController player;
    private Coroutine thinking;

    // The values shown now, to notice when something changes.
    private bool shownHasBottle;
    private int shownHealth;
    private int captures;

    private void Start()
    {
        CreateVignette();
        Canvas canvas = UiFactory.CreateCanvas("HUD Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        CreateHope(canvas.transform);
        CreateThought(canvas.transform);
        CreateBlackout();
        hints = FindAnyObjectByType<HintToast>();
        player = FindAnyObjectByType<PlayerController>();

        GameManager game = GameManager.Instance;
        if (game != null)
        {
            game.Captured += OnCaptured;
            game.BottlePicked += OnBottlePicked;
            shownHealth = game.Health;
            hopeFill.fillAmount = (float)game.Health / game.MaxHealth;
        }
        StartThought(FindWater, 14f); // After the opening, the first thought appears.
    }

    // The GameManager lives on after this scene, so we must stop listening, or it would call a destroyed HUD.
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Captured -= OnCaptured;
            GameManager.Instance.BottlePicked -= OnBottlePicked;
        }
    }

    private void Update()
    {
        // The flame breathes a little, like a real flame in still air.
        float flicker = 1f + (Mathf.PerlinNoise(Time.time * 2f, 0f) - 0.5f) * 0.12f;
        flame.localScale = new Vector3(1f, flicker, 1f);

        GameManager game = GameManager.Instance;
        if (game == null)
        {
            return;
        }

        float goal = (float)game.Health / game.MaxHealth;
        hopeFill.fillAmount = Mathf.MoveTowards(hopeFill.fillAmount, goal, barSpeed * Time.deltaTime);

        if (game.Health > shownHealth)
        {
            StartCoroutine(UiAnim.Punch(flame, 0.35f, 0.6f)); // Hope grew: the flame flares up.
        }
        shownHealth = game.Health;

        if (game.HasBottle != shownHasBottle)
        {
            shownHasBottle = game.HasBottle;
            StartCoroutine(UiAnim.FadeGraphic(waterIcon, game.HasBottle ? 1f : 0.18f, 0.6f));
        }
    }

    private void OnBottlePicked()
    {
        Sfx.Play("Water", 0.8f);
        StartCoroutine(UiAnim.Punch(waterIcon.transform, 0.3f, 0.5f));
        StartThought(GoBack, 0.5f);
        ShowHint("Water. Only half a bottle left in it.\nI'll drink it in the cell, where it's safe.");
    }

    private void OnCaptured()
    {
        // HasBottle is already false here, so the last shown value tells if the bottle was dropped.
        bool droppedBottle = shownHasBottle;
        captures++;
        Sfx.Play("Caught", 0.9f);
        ScreenShake.Shake(0.12f, 0.5f);
        StartCoroutine(Blackout(droppedBottle));
    }

    /// <summary>
    /// The camera already sent the player back to the cell. The darkness hides the jump,
    /// and gives the player a moment, like the hero, to breathe before trying again.
    /// </summary>
    private IEnumerator Blackout(bool droppedBottle)
    {
        SetPlayerFree(false);
        blackoutText.text = captures == 1 ? "They saw me." : "Again...";
        yield return UiAnim.Fade(blackout, 1f, 0.5f);
        yield return new WaitForSeconds(blackoutSeconds);
        yield return UiAnim.Fade(blackout, 0f, 1.5f);
        SetPlayerFree(true);

        if (droppedBottle)
        {
            StartThought(FindWater, 0f);
            ShowHint("They took the bottle back to the storage room.\nI'll try again. Slower this time.");
        }
        else if (captures == 1)
        {
            ShowHint("Back in the cell. Breathe.\nThe pale light is the camera's eye. I wait in the dark until it turns away.");
        }
        else
        {
            ShowHint("Breathe. Watch the light. Wait for it to turn.");
        }
    }

    private void SetPlayerFree(bool free)
    {
        if (player == null)
        {
            return;
        }
        player.enabled = free;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }

    private void ShowHint(string text)
    {
        if (hints != null)
        {
            hints.Show(text);
        }
    }

    /// <summary>A new thought replaces the one being written, so two thoughts never mix.</summary>
    private void StartThought(string text, float delay)
    {
        if (thinking != null)
        {
            StopCoroutine(thinking);
        }
        thinking = StartCoroutine(Think(text, delay));
    }

    /// <summary>Writes a new thought under the hope bar, after a delay.</summary>
    private IEnumerator Think(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return UiAnim.Typewriter(thought, text, 18f);
    }

    private void CreateVignette()
    {
        Canvas canvas = UiFactory.CreateCanvas("Vignette Canvas", VignetteSortingOrder);
        canvas.transform.SetParent(transform, false);
        Image vignette = UiFactory.CreateImage(canvas.transform, "Vignette", UiTheme.Sprite("Vignette"), new Color(1f, 1f, 1f, 0.7f));
        UiFactory.Stretch(vignette.rectTransform);
    }

    /// <summary>A small flame, a thin bar beside it, and the water drop at the end of the bar.</summary>
    private void CreateHope(Transform canvas)
    {
        Image flameIcon = UiFactory.CreateIcon(canvas, "Hope", new Vector2(64f, 64f));
        flame = flameIcon.rectTransform;
        UiFactory.Place(flame, TopLeft, new Vector2(70f, -76f), new Vector2(64f, 64f));
        flame.pivot = new Vector2(0.5f, 0f); // It flickers from its base, like a real flame.

        Text label = UiFactory.CreateText(canvas, "Label", TopLeft, new Vector2(200f, 30f), 26, TextAnchor.MiddleLeft);
        label.rectTransform.anchoredPosition = new Vector2(112f, -36f);
        label.color = UiTheme.Muted;
        label.text = "hope";

        Image barBack = UiFactory.CreatePanel(canvas, "Hope Bar", "BarBack");
        UiFactory.Place(barBack.rectTransform, TopLeft, new Vector2(112f, -74f), new Vector2(280f, 12f));

        hopeFill = UiFactory.CreateImage(barBack.transform, "Fill", UiTheme.Sprite("BarFill"), UiTheme.Hope);
        UiFactory.Stretch(hopeFill.rectTransform);
        hopeFill.type = Image.Type.Filled; // fillAmount from 0 to 1 cuts the picture from the right.
        hopeFill.fillMethod = Image.FillMethod.Horizontal;

        waterIcon = UiFactory.CreateIcon(canvas, "Drop", new Vector2(40f, 40f));
        UiFactory.Place(waterIcon.rectTransform, TopLeft, new Vector2(428f, -60f), new Vector2(40f, 40f));
        waterIcon.rectTransform.pivot = Center;
        waterIcon.color = new Color(1f, 1f, 1f, 0.18f); // Faint = the hero has no water.
    }

    private void CreateThought(Transform canvas)
    {
        thought = UiFactory.CreateText(canvas, "Thought", TopLeft, new Vector2(900f, 50f), 32, TextAnchor.MiddleLeft);
        thought.rectTransform.anchoredPosition = new Vector2(40f, -112f);
        thought.text = "";
    }

    /// <summary>A black screen with one line of text, hidden until the player is caught.</summary>
    private void CreateBlackout()
    {
        Canvas canvas = UiFactory.CreateCanvas("Blackout", BlackoutSortingOrder);
        canvas.transform.SetParent(transform, false);
        blackout = canvas.gameObject.AddComponent<CanvasGroup>();
        blackout.alpha = 0f;
        blackout.blocksRaycasts = false;

        Image black = UiFactory.CreateImage(canvas.transform, "Black", null, new Color(0.02f, 0f, 0f, 0.96f));
        UiFactory.Stretch(black.rectTransform);

        blackoutText = UiFactory.CreateTitle(canvas.transform, "Line", Center, new Vector2(1200f, 120f), 56, UiTheme.TextColor);
        blackoutText.rectTransform.pivot = Center;
        blackoutText.font = UiTheme.ItalicFont;
    }
}
