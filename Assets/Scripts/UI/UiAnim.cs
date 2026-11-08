using System.Collections;
using UnityEngine;

/// <summary>
/// Small animations for the UI, written as coroutines: start one with StartCoroutine(UiAnim.Pop(...)).
/// Each one changes a value a little every frame until the time is up.
/// </summary>
public static class UiAnim
{
    /// <summary>Fades a whole group of UI objects (alpha 0 = invisible, 1 = visible).</summary>
    public static IEnumerator Fade(CanvasGroup group, float to, float seconds)
    {
        float from = group.alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }

    /// <summary>Grows from nothing to a bit too big, then settles. Makes things "pop" onto the screen.</summary>
    public static IEnumerator Pop(Transform target, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            target.localScale = Vector3.one * EaseOutBack(t / seconds);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    /// <summary>A quick bump in size and back, to draw the eye to something that changed.</summary>
    public static IEnumerator Punch(Transform target, float amount, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            // Sin goes up and back down to 0 over half a turn.
            target.localScale = Vector3.one * (1f + Mathf.Sin(t / seconds * Mathf.PI) * amount);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    /// <summary>Shakes a UI object around its place, weaker and weaker until it stops.</summary>
    public static IEnumerator Shake(RectTransform target, float strength, float seconds)
    {
        Vector2 home = target.anchoredPosition;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float fade = 1f - t / seconds;
            target.anchoredPosition = home + Random.insideUnitCircle * strength * fade;
            yield return null;
        }
        target.anchoredPosition = home;
    }

    /// <summary>Slides a UI object to a place, fast at first and slow at the end.</summary>
    public static IEnumerator Slide(RectTransform target, Vector2 to, float seconds)
    {
        Vector2 from = target.anchoredPosition;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / seconds, 3f); // Ease out: slows down near the end.
            target.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        target.anchoredPosition = to;
    }

    /// <summary>
    /// Writes the text one letter at a time, like someone typing or thinking it.
    /// A short stop after punctuation gives the words a human rhythm.
    /// </summary>
    public static IEnumerator Typewriter(UnityEngine.UI.Text target, string text, float lettersPerSecond)
    {
        target.text = "";
        for (int i = 0; i < text.Length; i++)
        {
            target.text = text.Substring(0, i + 1);
            char letter = text[i];
            bool pause = letter == '.' || letter == ',' || letter == '\n';
            yield return new WaitForSeconds(pause ? 0.25f : 1f / lettersPerSecond);
        }
    }

    /// <summary>Fades one UI element (text or image) to an alpha.</summary>
    public static IEnumerator FadeGraphic(UnityEngine.UI.Graphic target, float to, float seconds)
    {
        Color color = target.color;
        float from = color.a;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            color.a = Mathf.Lerp(from, to, t / seconds);
            target.color = color;
            yield return null;
        }
        color.a = to;
        target.color = color;
    }

    /// <summary>Goes a little past 1 and comes back, like a spring.</summary>
    private static float EaseOutBack(float x)
    {
        const float overshoot = 1.7f;
        float a = x - 1f;
        return 1f + (overshoot + 1f) * a * a * a + overshoot * a * a;
    }
}
