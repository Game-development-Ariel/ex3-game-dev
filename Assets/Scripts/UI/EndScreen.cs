using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The screen after the level, built like the last shot of a film: darkness, then a beam of light from above
/// (the "light in the tunnel"), the day's title, the hero's last thoughts written one by one,
/// and the results of the day. The UI is created here in code, when the scene starts.
/// </summary>
public class EndScreen : MonoBehaviour
{
    private const int CanvasSortingOrder = 0;

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    [SerializeField] private string[] winThoughts = { "I drank slowly. Small sips.", "Somewhere above, people are looking for us.", "Tomorrow I'll look for the way out." };
    [SerializeField] private string[] loseThoughts = { "The darkness was too heavy today.", "I'll rest. Tomorrow I'll try again." };

    private Transform canvas;

    private IEnumerator Start()
    {
        UiFactory.EnsureEventSystem();
        canvas = UiFactory.CreateCanvas("End Canvas", CanvasSortingOrder).transform;
        Image background = UiFactory.CreateImage(canvas, "Background", null, new Color(0.02f, 0.02f, 0.03f));
        UiFactory.Stretch(background.rectTransform);

        GameManager game = GameManager.Instance;
        bool won = game == null || game.Won;
        Sfx.Play(won ? "Win" : "Lose", 0.9f);

        // The beam: the brighter the ending, the more light comes in.
        Image beam = UiFactory.CreateImage(canvas, "Beam", UiTheme.Sprite("Beam"), new Color(1f, 1f, 1f, 0f));
        UiFactory.Place(beam.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 1100f));
        StartCoroutine(UiAnim.FadeGraphic(beam, won ? 0.4f : 0.12f, 4f));
        Image vignette = UiFactory.CreateImage(canvas, "Vignette", UiTheme.Sprite("Vignette"), Color.white);
        UiFactory.Stretch(vignette.rectTransform);
        yield return new WaitForSeconds(1.2f);

        Text title = AddCentered(UiFactory.CreateTitle(canvas, "Title", Center, new Vector2(1400f, 140f), 104, UiTheme.TextColor), 300f);
        Text subtitle = AddCentered(UiFactory.CreateTitle(canvas, "Subtitle", Center, new Vector2(1400f, 70f), 44, UiTheme.TextColor), 210f);
        subtitle.font = UiTheme.ItalicFont;

        if (game == null)
        {
            // Happens only when this scene is played directly in the editor, without the level before it.
            title.text = "The End";
            subtitle.text = "Start from the Tutorial scene to see the results.";
            yield break;
        }

        title.color = new Color(title.color.r, title.color.g, title.color.b, 0f);
        subtitle.color = new Color(subtitle.color.r, subtitle.color.g, subtitle.color.b, 0f);
        title.text = "Day 1";
        subtitle.text = won ? "I made it through the first day." : "Not today.";
        yield return FadeIn(title, 1.6f);
        yield return FadeIn(subtitle, 1.2f);
        yield return new WaitForSeconds(0.6f);

        string[] thoughts = won ? winThoughts : loseThoughts;
        for (int i = 0; i < thoughts.Length; i++)
        {
            Text line = AddCentered(UiFactory.CreateText(canvas, "Thought", Center, new Vector2(1400f, 50f), 34, TextAnchor.MiddleCenter), 70f - i * 58f);
            yield return UiAnim.Typewriter(line, thoughts[i], 26f);
            yield return new WaitForSeconds(0.7f);
        }

        yield return ShowResults(game, -170f);

        Button again = UiFactory.CreateButton(canvas, "Play again", Center, new Vector2(380f, 84f), 40, Color.white);
        ((RectTransform)again.transform).anchoredPosition = new Vector2(0f, -320f);
        again.onClick.AddListener(game.PlayAgain);
        var group = again.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        yield return UiAnim.Fade(group, 1f, 1.2f);
    }

    /// <summary>
    /// One quiet row: hope, times seen, and the time, each with a small icon.
    /// Layout groups place them: each result sizes itself to its text, and the row keeps equal gaps between them.
    /// </summary>
    private IEnumerator ShowResults(GameManager game, float y)
    {
        RectTransform row = CreateRow(canvas, "Results", 70f);
        UiFactory.Place(row, Center, new Vector2(0f, y), new Vector2(1200f, 60f));
        row.pivot = Center;

        string seen = game.Captures == 0 ? "never seen" : game.Captures == 1 ? "seen once" : $"seen {game.Captures} times";
        AddResult(row, "Hope", $"hope {game.Health}");
        AddResult(row, "Eye", seen);
        AddResult(row, "Clock", $"{game.LevelSeconds:0} seconds");

        var group = row.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        yield return UiAnim.Fade(group, 1f, 1.2f);
    }

    private static void AddResult(Transform row, string icon, string value)
    {
        RectTransform item = CreateRow(row, icon, 14f);

        Image image = UiFactory.CreateIcon(item, icon, new Vector2(40f, 40f));
        image.color = new Color(1f, 1f, 1f, 0.75f);

        Text text = UiFactory.CreateText(item, icon + " Value", Center, new Vector2(0f, 50f), 30, TextAnchor.MiddleLeft);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.color = UiTheme.Muted;
        text.text = value;
        text.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>An object that lines up its children from left to right, centered, with a gap between them.</summary>
    private static RectTransform CreateRow(Transform parent, string name, float spacing)
    {
        var rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        rowObject.transform.SetParent(parent, false);
        var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        rowObject.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rowObject.GetComponent<RectTransform>();
    }

    private static Text AddCentered(Text text, float y)
    {
        text.rectTransform.pivot = Center;
        text.rectTransform.anchoredPosition = new Vector2(0f, y);
        return text;
    }

    private static IEnumerator FadeIn(Text text, float seconds)
    {
        Color color = text.color;
        color.a = 0f;
        text.color = color;
        yield return UiAnim.FadeGraphic(text, 1f, seconds);
    }
}
