using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Small helpers that create UI objects in code: a canvas, a text and a button.
/// The HUD and the end screen use them, so both look the same.
/// </summary>
public static class UiFactory
{
    // The UI is designed for this screen size, and the CanvasScaler scales it to the real screen.
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    // 0.5 = scale by both width and height equally, so it works in landscape and portrait.
    private const float WidthHeightMatch = 0.5f;

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = WidthHeightMatch;
        return canvas;
    }

    /// <summary>
    /// Creates a text. The anchor is a point from (0,0) bottom left to (1,1) top right of the parent,
    /// and the text's box is placed at that point.
    /// </summary>
    public static Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 size, int fontSize, TextAnchor alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    public static Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 size, int fontSize, Color color)
    {
        var buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        buttonObject.GetComponent<Image>().color = color;

        Text text = CreateText(buttonObject.transform, "Label", new Vector2(0.5f, 0.5f), size, fontSize, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = Color.black;
        return buttonObject.GetComponent<Button>();
    }

    /// <summary>
    /// Buttons need an EventSystem to receive clicks and touches.
    /// The project uses the new Input System, so it needs the matching input module.
    /// </summary>
    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
