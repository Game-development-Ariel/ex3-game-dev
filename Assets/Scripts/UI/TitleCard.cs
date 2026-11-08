using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The opening of the level, from the design: a black screen and the sound of breathing.
/// The hero's first thoughts are written in the dark, then the day's title,
/// and then the eyes open: two black eyelids part, close once more, and open for real.
/// </summary>
public class TitleCard : MonoBehaviour
{
    // Above the HUD and the hints, below the scene fader.
    private const int CanvasSortingOrder = 50;

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    [SerializeField] private string[] thoughts = { "I don't know how long I slept.", "It's dark. My throat is burning." };
    [SerializeField] private string title = "Day 1";
    [SerializeField] private string subtitle = "The Cell";

    [SerializeField] private float lettersPerSecond = 22f;

    private RectTransform canvasRect;

    private IEnumerator Start()
    {
        Canvas canvas = UiFactory.CreateCanvas("Title Card", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        canvasRect = canvas.GetComponent<RectTransform>();

        Image black = UiFactory.CreateImage(canvas.transform, "Black", null, Color.black);
        UiFactory.Stretch(black.rectTransform);
        Sfx.Play("Wake", 0.8f);
        yield return new WaitForSeconds(1.5f);

        yield return WriteThoughts(canvas.transform);
        yield return ShowTitle(canvas.transform);

        // The eyelids replace the black screen. Each covers half of the screen and shrinks to open.
        RectTransform top = CreateEyelid(canvas.transform, new Vector2(0.5f, 1f));
        RectTransform bottom = CreateEyelid(canvas.transform, new Vector2(0.5f, 0f));
        Destroy(black.gameObject);
        yield return OpenEyes(top, bottom);
        Destroy(canvas.gameObject);

        ShowMoveHint();
    }

    private IEnumerator WriteThoughts(Transform canvas)
    {
        var lines = new Text[thoughts.Length];
        for (int i = 0; i < thoughts.Length; i++)
        {
            lines[i] = UiFactory.CreateText(canvas, "Thought", Center, new Vector2(1400f, 60f), 40, TextAnchor.MiddleCenter);
            lines[i].rectTransform.pivot = Center;
            lines[i].rectTransform.anchoredPosition = new Vector2(0f, 40f - i * 70f);
            yield return UiAnim.Typewriter(lines[i], thoughts[i], lettersPerSecond);
            yield return new WaitForSeconds(0.6f);
        }
        yield return new WaitForSeconds(1f);
        foreach (Text line in lines)
        {
            StartCoroutine(UiAnim.FadeGraphic(line, 0f, 1.2f));
        }
        yield return new WaitForSeconds(1.4f);
    }

    private IEnumerator ShowTitle(Transform canvas)
    {
        Text titleText = UiFactory.CreateTitle(canvas, "Title", Center, new Vector2(1400f, 140f), 110, UiTheme.TextColor);
        titleText.rectTransform.pivot = Center;
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 50f);
        titleText.text = title;

        Text subtitleText = UiFactory.CreateTitle(canvas, "Subtitle", Center, new Vector2(1400f, 60f), 44, UiTheme.Muted);
        subtitleText.rectTransform.pivot = Center;
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, -40f);
        subtitleText.font = UiTheme.ItalicFont;
        subtitleText.text = subtitle;

        foreach (Text text in new[] { titleText, subtitleText })
        {
            Color color = text.color;
            color.a = 0f;
            text.color = color;
            StartCoroutine(UiAnim.FadeGraphic(text, 1f, 1.5f));
        }
        yield return new WaitForSeconds(3f);
        StartCoroutine(UiAnim.FadeGraphic(titleText, 0f, 1.2f));
        yield return UiAnim.FadeGraphic(subtitleText, 0f, 1.2f);
    }

    private RectTransform CreateEyelid(Transform canvas, Vector2 anchor)
    {
        Image lid = UiFactory.CreateImage(canvas, "Eyelid", null, Color.black);
        RectTransform rect = lid.rectTransform;
        rect.anchorMin = new Vector2(0f, anchor.y);
        rect.anchorMax = new Vector2(1f, anchor.y);
        rect.pivot = anchor;
        rect.sizeDelta = new Vector2(0f, HalfScreen());
        return rect;
    }

    /// <summary>Half open... closed again... then slowly open. Like waking up in a place you don't want to be.</summary>
    private IEnumerator OpenEyes(RectTransform top, RectTransform bottom)
    {
        float half = HalfScreen();
        yield return MoveLids(top, bottom, half, half * 0.7f, 1.6f);
        yield return new WaitForSeconds(0.4f);
        yield return MoveLids(top, bottom, half * 0.7f, half, 0.35f);
        yield return new WaitForSeconds(0.5f);
        yield return MoveLids(top, bottom, half, 0f, 2.2f);
    }

    private IEnumerator MoveLids(RectTransform top, RectTransform bottom, float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / seconds); // Slow at the start and the end, like real eyelids.
            float height = Mathf.Lerp(from, to, k);
            top.sizeDelta = new Vector2(0f, height);
            bottom.sizeDelta = new Vector2(0f, height);
            yield return null;
        }
        top.sizeDelta = new Vector2(0f, to);
        bottom.sizeDelta = new Vector2(0f, to);
    }

    // A bit more than half, so the two lids overlap and no light leaks between them.
    private float HalfScreen()
    {
        return canvasRect.rect.height / 2f + 20f;
    }

    private static void ShowMoveHint()
    {
        HintToast hints = FindAnyObjectByType<HintToast>();
        if (hints == null)
        {
            return;
        }

        if (MobileJoystick.UsesTouch)
        {
            hints.Show("I can move, slowly. Drag the circle in the corner.\nFirst, let me look around the cell.");
        }
        else
        {
            hints.Show("I can move, slowly. These keys, or the arrows.\nFirst, let me look around the cell.", "W", "A", "S", "D");
        }
    }
}
