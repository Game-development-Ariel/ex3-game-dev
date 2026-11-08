using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The tutorial's quiet teacher: the hero's thoughts, written at the bottom of the screen like subtitles.
/// The text types itself out, stays a few seconds, and fades away. Keyboard keys can be shown above it.
/// Call FindAnyObjectByType&lt;HintToast&gt;().Show("text").
/// </summary>
public class HintToast : MonoBehaviour
{
    // Above the HUD (10), below the title card and the fader.
    private const int CanvasSortingOrder = 20;

    private const float KeySize = 58f;

    [Tooltip("Seconds a thought stays after it is fully written")]
    [SerializeField] private float showSeconds = 5f;

    [Tooltip("How fast the words are written")]
    [SerializeField] private float lettersPerSecond = 40f;

    private CanvasGroup group;
    private Text message;
    private RectTransform keyRow;
    private Coroutine current;

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Hint Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        // A dark band that fades out at its ends, so the words are readable without a hard box.
        Image band = UiFactory.CreateImage(canvas.transform, "Band", UiTheme.Sprite("Band"), Color.white);
        UiFactory.Place(band.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1500f, 150f));

        message = UiFactory.CreateText(band.transform, "Message", new Vector2(0.5f, 0.5f), new Vector2(1250f, 140f), 34, TextAnchor.MiddleCenter);
        message.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        var row = new GameObject("Keys", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(band.transform, false);
        keyRow = row.GetComponent<RectTransform>();
        UiFactory.Place(keyRow, new Vector2(0.5f, 1f), new Vector2(0f, 45f), new Vector2(600f, KeySize));
        keyRow.pivot = new Vector2(0.5f, 0.5f);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false; // Keeps the keys together in the middle.
    }

    /// <summary>Shows a thought. Keys are labels like "W", drawn as keyboard keys above the text.</summary>
    public void Show(string text, params string[] keys)
    {
        if (current != null)
        {
            StopCoroutine(current);
        }
        SetKeys(keys);
        current = StartCoroutine(ShowRoutine(text));
    }

    private IEnumerator ShowRoutine(string text)
    {
        Sfx.Play("Hint", 0.5f);
        message.text = "";
        yield return UiAnim.Fade(group, 1f, 0.4f);
        yield return UiAnim.Typewriter(message, text, lettersPerSecond);
        yield return new WaitForSeconds(showSeconds);
        yield return UiAnim.Fade(group, 0f, 1.2f);
    }

    private void SetKeys(string[] keys)
    {
        foreach (Transform old in keyRow)
        {
            Destroy(old.gameObject);
        }
        foreach (string key in keys)
        {
            Image cap = UiFactory.CreatePanel(keyRow, "Key " + key, "Key");
            cap.rectTransform.sizeDelta = Vector2.one * KeySize;
            Text label = UiFactory.CreateText(cap.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.one * KeySize, 26, TextAnchor.MiddleCenter);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.text = key;
        }
    }
}
