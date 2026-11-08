using UnityEngine;

/// <summary>
/// The look of the game's UI in one place: fonts, colors and sprites.
/// The game tells a hard, true-inspired story, so the look is quiet: dark, warm, and plain.
/// The files are in Assets/Resources, so code can load them by name without Inspector fields.
/// </summary>
public static class UiTheme
{
    public static readonly Color TextColor = new Color(0.92f, 0.89f, 0.84f);
    public static readonly Color Muted = new Color(0.6f, 0.58f, 0.55f);
    public static readonly Color Hope = new Color(1f, 0.78f, 0.5f);
    public static readonly Color Water = new Color(0.62f, 0.76f, 0.88f);
    public static readonly Color Danger = new Color(0.72f, 0.16f, 0.13f);

    /// <summary>An old book serif, for titles and memories.</summary>
    public static Font TitleFont => Resources.Load<Font>("Fonts/CrimsonText");

    /// <summary>The same serif in italics, for memories and quiet moments.</summary>
    public static Font ItalicFont => Resources.Load<Font>("Fonts/CrimsonTextItalic");

    /// <summary>A typewriter font, for the hero's thoughts, like a diary.</summary>
    public static Font ThoughtFont => Resources.Load<Font>("Fonts/CourierPrime");

    /// <summary>A sprite from Assets/Resources/UI, for example "Hope".</summary>
    public static Sprite Sprite(string name)
    {
        return Resources.Load<Sprite>("UI/" + name);
    }
}
